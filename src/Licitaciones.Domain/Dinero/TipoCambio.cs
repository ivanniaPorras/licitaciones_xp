namespace Licitaciones.Domain.Dinero;

/// <summary>
/// Cantidad de colones equivalente a un dólar, con la fecha desde la que rige. Solo un
/// registro puede estar activo a la vez.
/// </summary>
public sealed class TipoCambio
{
    private const int DecimalesPermitidos = 4;

    private TipoCambio(decimal crcPorUsd, DateTimeOffset fechaVigencia)
    {
        CRCporUSD = crcPorUsd;
        FechaVigencia = fechaVigencia;
    }

    /// <summary>Identificador generado por el sistema.</summary>
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>Colones equivalentes a un dólar.</summary>
    public decimal CRCporUSD { get; private set; }

    /// <summary>Fecha desde la que rige la tasa. Se muestra junto a cada monto convertido.</summary>
    public DateTimeOffset FechaVigencia { get; private set; }

    /// <summary>Indica si esta es la tasa que el sistema usa para convertir.</summary>
    public bool Activo { get; private set; }

    /// <summary>Crea un tipo de cambio inactivo tras comprobar que la tasa es positiva.</summary>
    /// <param name="crcPorUsd">Colones equivalentes a un dólar.</param>
    /// <param name="fechaVigencia">Fecha desde la que rige.</param>
    /// <exception cref="MontoInvalidoException">
    /// Si la tasa no es mayor que cero o tiene más de cuatro decimales.
    /// </exception>
    public static TipoCambio Crear(decimal crcPorUsd, DateTimeOffset fechaVigencia)
    {
        if (crcPorUsd <= 0m)
        {
            throw new MontoInvalidoException("El tipo de cambio debe ser mayor que cero.");
        }

        // La tasa admite más precisión que un monto porque es un factor de conversión y no
        // una cantidad de dinero que deba cuadrar al céntimo.
        if (crcPorUsd.Scale > DecimalesPermitidos)
        {
            throw new MontoInvalidoException("El tipo de cambio no puede tener más de cuatro decimales.");
        }

        return new TipoCambio(crcPorUsd, fechaVigencia);
    }

    /// <summary>Marca esta tasa como la vigente.</summary>
    public void Activar() => Activo = true;

    /// <summary>Deja de usar esta tasa para convertir.</summary>
    public void Desactivar() => Activo = false;
}
