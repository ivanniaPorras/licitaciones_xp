using Licitaciones.Application.Moneda;
using Microsoft.AspNetCore.Mvc;

namespace Licitaciones.Web.ViewComponents;

/// <summary>
/// Publica la tasa vigente en la barra de navegación para que cualquier pantalla pueda
/// alternar entre colones y dólares.
/// </summary>
/// <remarks>
/// Es un componente de vista y no datos que cada controlador tenga que cargar: el
/// alternador vive en el diseño compartido y aparece en todas las pantallas, así que
/// obligar a los cinco controladores a proveerlo repetiría lo mismo cinco veces.
/// </remarks>
public sealed class AlternadorMonedaViewComponent : ViewComponent
{
    private readonly ITipoCambioService _tiposCambio;

    /// <summary>Crea el componente.</summary>
    /// <param name="tiposCambio">Casos de uso de tipos de cambio.</param>
    public AlternadorMonedaViewComponent(ITipoCambioService tiposCambio) =>
        _tiposCambio = tiposCambio;

    /// <summary>Devuelve la tasa vigente, o <c>null</c> si no hay ninguna activa.</summary>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    public async Task<IViewComponentResult> InvokeAsync(CancellationToken cancelacion = default)
    {
        var resultado = await _tiposCambio.ObtenerVigenteAsync(cancelacion);

        // Sin tasa activa el alternador no se dibuja, pero la página se sigue mostrando en
        // colones: la conversión es una representación añadida, no un requisito para leer.
        return View(resultado.EsCorrecto ? resultado.Valor : null);
    }
}
