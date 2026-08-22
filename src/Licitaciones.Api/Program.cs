using Asp.Versioning;
using Licitaciones.Api.Errores;
using Licitaciones.Application;
using Licitaciones.Infrastructure;
using Microsoft.AspNetCore.Mvc;

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

var app = builder.Build();

// El middleware va antes que nada: cualquier fallo no previsto sale como ProblemDetails
// sin traza de pila ni detalles internos.
app.UseMiddleware<MiddlewareExcepciones>();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

/// <summary>
/// Punto de entrada de la API. Se declara público para que las pruebas de integración
/// puedan levantarla con <c>WebApplicationFactory</c>.
/// </summary>
public partial class Program;
