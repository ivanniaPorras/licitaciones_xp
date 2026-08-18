using System.Globalization;
using Licitaciones.Application;
using Licitaciones.Infrastructure;
using Microsoft.AspNetCore.Localization;

var builder = WebApplication.CreateBuilder(args);

// La cadena de conexión llega por variable de entorno (ConnectionStrings__Default). No
// hay credenciales en ningún archivo versionado.
var cadenaConexion = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException(
        "Falta la cadena de conexión. Defina la variable de entorno ConnectionStrings__Default.");

builder.Services.AddControllersWithViews();
builder.Services.AgregarAplicacion();
builder.Services.AgregarInfraestructura(cadenaConexion);

var app = builder.Build();

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

app.Run();

/// <summary>
/// Punto de entrada de la aplicación web. Se declara público para que las pruebas
/// funcionales puedan levantarla con <c>WebApplicationFactory</c>.
/// </summary>
public partial class Program;
