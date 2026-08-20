using Licitaciones.Application.Comun;
using Licitaciones.Application.Persistencia;
using Licitaciones.Domain.Dinero;

namespace Licitaciones.Application.Moneda;

/// <inheritdoc cref="IConversionMonedaService" />
/// <remarks>
/// La conversión no se persiste nunca. Guardar el equivalente en dólares crearía un
/// segundo valor que quedaría desactualizado en cuanto cambiara la tasa, y abriría la
/// puerta a que dos partes del sistema informaran cifras distintas para el mismo monto.
/// </remarks>
public sealed class ConversionMonedaService : IConversionMonedaService
{
    private const int DecimalesDelMontoConvertido = 2;

    private readonly ITipoCambioRepository _tiposCambio;

    /// <summary>Crea el servicio con su acceso a las tasas.</summary>
    /// <param name="tiposCambio">Acceso a los tipos de cambio.</param>
    public ConversionMonedaService(ITipoCambioRepository tiposCambio) => _tiposCambio = tiposCambio;

    /// <inheritdoc />
    public async Task<Result<ConversionResponse>> ConvertirAsync(
        decimal montoCRC,
        CancellationToken cancelacion = default)
    {
        MontoCRC monto;
        try
        {
            monto = MontoCRC.Crear(montoCRC);
        }
        catch (MontoInvalidoException error)
        {
            return Result<ConversionResponse>.Fallo(
                ErrorAplicacion.Validacion(CodigosError.MontoInvalido, error.Message));
        }

        var vigente = await _tiposCambio.ObtenerActivoAsync(cancelacion);
        if (vigente is null)
        {
            return Result<ConversionResponse>.Fallo(ErrorAplicacion.Conflicto(
                CodigosError.SinTipoCambioActivo,
                "No hay un tipo de cambio activo para realizar la conversión."));
        }

        // Se redondea alejándose de cero y no al par más cercano, que es lo que hace
        // Math.Round por omisión: con el redondeo al par, un mismo monto podría mostrarse
        // hacia arriba o hacia abajo según el dígito anterior.
        var montoUSD = Math.Round(
            monto.Valor / vigente.CRCporUSD,
            DecimalesDelMontoConvertido,
            MidpointRounding.AwayFromZero);

        return Result<ConversionResponse>.Correcto(new ConversionResponse(
            monto.Valor,
            montoUSD,
            vigente.CRCporUSD,
            vigente.FechaVigencia));
    }
}
