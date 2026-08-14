using System.Diagnostics;
using Licitaciones.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace Licitaciones.Web.Controllers;

/// <summary>
/// Controlador de la página inicial y de la vista de error genérica.
/// </summary>
public sealed class HomeController : Controller
{
    /// <summary>Presenta la página inicial del sistema.</summary>
    public IActionResult Index() => View();

    /// <summary>
    /// Presenta la vista de error sin exponer detalles técnicos, adjuntando únicamente
    /// el identificador de correlación de la solicitud.
    /// </summary>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel
    {
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
    });
}
