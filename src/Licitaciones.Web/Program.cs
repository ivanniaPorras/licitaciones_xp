using System.Globalization;
using Licitaciones.Application;
using Licitaciones.Infrastructure;
using Licitaciones.Infrastructure.Persistencia;
using Licitaciones.Infrastructure.Salud;
using Licitaciones.Web.Vistas;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Localization;

var builder = WebApplication.CreateBuilder(args);

// La cadena de conexión llega por variable de entorno (ConnectionStrings__Default). No
// hay credenciales en ningún archivo versionado.
var cadenaConexion = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException(
        "Falta la cadena de conexión. Defina la variable de entorno ConnectionStrings__Default.");

// Los campos numéricos del navegador envían siempre el punto como separador decimal,
// aunque la aplicación se muestre con la cultura de Costa Rica.
builder.Services.AddControllersWithViews(opciones =>
    opciones.ModelBinderProviders.Insert(0, new ProveedorEnlazadorDecimalInvariante()));
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

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Cultura de Costa Rica para que los colones se presenten como ₡1.250.000,00.
var cultura = new CultureInfo("es-CR");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(cultura),
    SupportedCultures = [cultura],
    SupportedUICultures = [cultura]
});

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();
app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Vitalidad: el proceso responde. Disponibilidad: además alcanza la base.
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = comprobacion => comprobacion.Tags.Contains(RegistroComprobacionesSalud.EtiquetaBaseDatos)
});

await app.RunAsync();

return 0;

/// <summary>
/// Punto de entrada de la aplicación web. Se declara público para que las pruebas
/// funcionales puedan levantarla con <c>WebApplicationFactory</c>.
/// </summary>
public partial class Program;
