using Licitaciones.Application.Comun;

namespace Licitaciones.Application.Moneda;

/// <summary>Conversión de montos a dólares contra la tasa vigente.</summary>
public interface IConversionMonedaService
{
    /// <summary>
    /// Expresa en dólares un monto en colones, devolviendo también la tasa usada y su
    /// fecha de vigencia.
    /// </summary>
    /// <param name="montoCRC">Monto en colones que se quiere leer en dólares.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    Task<Result<ConversionResponse>> ConvertirAsync(
        decimal montoCRC,
        CancellationToken cancelacion = default);
}
