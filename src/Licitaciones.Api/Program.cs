using Asp.Versioning;
using Licitaciones.Api.Errores;
using Licitaciones.Application;
using Licitaciones.Infrastructure;
using Licitaciones.Infrastructure.Persistencia;
using Licitaciones.Infrastructure.Salud;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

var cadenaConexion = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException(
        "Falta la cadena de conexión. Defina la variable de entorno ConnectionStrings__Default.");

builder.Services.AddControllers();
builder.Services.AddProblemDetails();

// Una solicitud que no se puede interpretar responde con la misma forma que el resto de
// los errores del sistema, con su código propio y su identificador de correlación.
builder.Services.Configure<ApiBehaviorOptions>(opciones =>
    opciones.InvalidModelStateResponseFactory = RespuestaSolicitudInvalida.Crear);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opciones =>
{
    opciones.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "API del Sistema de Gestión de Licitaciones",
        Version = "v1",
        Description =
            "Interfaz de programación del sistema de licitaciones. Todas las rutas viven "
            + "bajo api/v1 y devuelven objetos de transferencia, nunca entidades de "
            + "persistencia.\n\n"
            + "Los montos se expresan siempre en colones costarricenses. La lectura en "
            + "dólares se obtiene con el recurso de conversión y no altera ningún dato "
            + "almacenado.\n\n"
            + "Los listados aceptan pagina, tamano, orden y busqueda, y responden con el "
            + "total de elementos y de páginas. Los errores se devuelven como "
            + "ProblemDetails con un código propio del dominio en la extensión code y un "
            + "identificador de correlación en correlationId."
    });

    // Los comentarios de los controladores son la única documentación de cada operación:
    // se generan al compilar y se incrustan aquí para no mantener dos textos distintos.
    var xml = Path.Combine(AppContext.BaseDirectory, "Licitaciones.Api.xml");
    if (File.Exists(xml))
    {
        opciones.IncludeXmlComments(xml);
    }
});

builder.Services.AddApiVersioning(opciones =>
{
    opciones.DefaultApiVersion = new ApiVersion(1, 0);
    opciones.AssumeDefaultVersionWhenUnspecified = true;
    opciones.ReportApiVersions = true;
}).AddMvc().AddApiExplorer(opciones => opciones.GroupNameFormat = "'v'VVV");

builder.Services.AgregarAplicacion();
builder.Services.AgregarInfraestructura(cadenaConexion);
builder.Services.AgregarComprobacionesSalud();

var app = builder.Build();

// Las migraciones se piden por argumento y corren en un paso aparte, no al arrancar: con
// varias réplicas, todas migrarían a la vez sobre la misma base.
if (MigradorBaseDatos.SePidioMigrar(args))
{
    return await MigradorBaseDatos.AplicarAsync(app.Services);
}

// El middleware va antes que nada: cualquier fallo no previsto sale como ProblemDetails
// sin traza de pila ni detalles internos.
app.UseMiddleware<MiddlewareExcepciones>();

app.UseSwagger();
app.UseSwaggerUI(opciones =>
{
    opciones.SwaggerEndpoint("/swagger/v1/swagger.json", "Licitaciones v1");
    opciones.DocumentTitle = "API de Licitaciones";
    opciones.DisplayRequestDuration();
});

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Vitalidad: el proceso responde. Disponibilidad: además alcanza la base.
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = comprobacion => comprobacion.Tags.Contains(RegistroComprobacionesSalud.EtiquetaBaseDatos)
});

await app.RunAsync();

return 0;

/// <summary>
/// Punto de entrada de la API. Se declara público para que las pruebas de integración
/// puedan levantarla con <c>WebApplicationFactory</c>.
/// </summary>
public partial class Program;
