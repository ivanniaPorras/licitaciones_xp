using Licitaciones.Domain.Dinero;

namespace Licitaciones.Domain.Ofertas;

/// <summary>
/// Calcula el ahorro que representa la mejor oferta frente al presupuesto estimado y lo
/// traduce a una de las cuatro etiquetas acordadas con el cliente.
/// </summary>
public static class ClasificadorAhorro
{
    /// <summary>Umbral a partir del cual el ahorro se considera conveniente. Es inclusivo.</summary>
    public const decimal UmbralOfertaConveniente = 10m;

    /// <summary>Etiqueta usada cuando la licitación no tiene ofertas válidas.</summary>
    public const string SinOfertasValidas = "Sin ofertas válidas";

    /// <summary>Etiqueta usada cuando el ahorro alcanza o supera el umbral.</summary>
    public const string OfertaConveniente = "Oferta conveniente";

    /// <summary>Etiqueta usada cuando hay ahorro pero no alcanza el umbral.</summary>
    public const string OfertaAceptable = "Oferta aceptable";

    /// <summary>Etiqueta usada cuando la oferta iguala al presupuesto.</summary>
    public const string OfertaValidaSinAhorro = "Oferta válida sin ahorro";

    /// <summary>Clasifica la mejor oferta respecto del presupuesto estimado.</summary>
    /// <param name="presupuesto">Presupuesto estimado de la licitación.</param>
    /// <param name="mejorOferta">
    /// Mejor oferta válida, o <c>null</c> si la licitación no tiene ninguna.
    /// </param>
    public static ResultadoClasificacion Clasificar(MontoCRC presupuesto, MontoCRC? mejorOferta)
    {
        if (mejorOferta is null)
        {
            return new ResultadoClasificacion(SinOfertasValidas, PorcentajeAhorro: null);
        }

        var ahorro = CalcularPorcentajeAhorro(presupuesto, mejorOferta.Value);

        // La ausencia de ahorro se decide comparando los montos y no el porcentaje: un
        // ahorro de un céntimo sobre un presupuesto grande redondea a 0,00 % y no por eso
        // deja de ser un ahorro.
        var etiqueta = mejorOferta.Value == presupuesto
            ? OfertaValidaSinAhorro
            : ahorro >= UmbralOfertaConveniente ? OfertaConveniente : OfertaAceptable;

        return new ResultadoClasificacion(etiqueta, ahorro);
    }

    private static decimal CalcularPorcentajeAhorro(MontoCRC presupuesto, MontoCRC mejorOferta)
    {
        var ahorro = (presupuesto.Valor - mejorOferta.Valor) / presupuesto.Valor * 100m;

        return Math.Round(ahorro, 2, MidpointRounding.AwayFromZero);
    }
}
