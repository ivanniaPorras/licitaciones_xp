using Licitaciones.Domain.Auditoria;

namespace Licitaciones.Domain.Dinero;

/// <summary>
/// Cantidad de colones equivalente a un dólar, con la fecha desde la que rige. Solo un
/// registro puede estar activo a la vez.
/// </summary>
public sealed class TipoCambio : IAuditable
{
    private const int DecimalesPermitidos = 4;

    // Constructor que Entity Framework Core usa al materializar la entidad.
    private TipoCambio()
    {
    }

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

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset UpdatedAt { get; private set; }

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

        return new TipoCambio(crcPorUsd, ADiaDeVigencia(fechaVigencia));
    }

    /// <summary>
    /// Conserva el día escrito y lo guarda a medianoche en tiempo universal. La vigencia
    /// es una fecha de calendario y no un instante: se toma el día tal como lo vio quien
    /// la escribió y se ancla en cero, de modo que el 1 de enero registrado desde Costa
    /// Rica y el registrado desde Europa queden guardados igual y se lean igual.
    /// </summary>
    private static DateTimeOffset ADiaDeVigencia(DateTimeOffset fechaVigencia) =>
        new(fechaVigencia.Date, TimeSpan.Zero);

    /// <summary>Cambia la tasa y la fecha desde la que rige, sin alterar quién está activo.</summary>
    /// <param name="crcPorUsd">Nueva cantidad de colones equivalente a un dólar.</param>
    /// <param name="fechaVigencia">Nueva fecha desde la que rige.</param>
    /// <exception cref="MontoInvalidoException">
    /// Si la tasa no es mayor que cero o tiene más de cuatro decimales.
    /// </exception>
    public void CambiarTasa(decimal crcPorUsd, DateTimeOffset fechaVigencia)
    {
        // Se construye un candidato para no duplicar las comprobaciones: si la tasa nueva
        // no es válida, la excepción salta antes de tocar el registro existente.
        var propuesto = Crear(crcPorUsd, fechaVigencia);

        CRCporUSD = propuesto.CRCporUSD;
        FechaVigencia = propuesto.FechaVigencia;
    }

    /// <summary>Marca esta tasa como la vigente.</summary>
    public void Activar() => Activo = true;

    /// <summary>Deja de usar esta tasa para convertir.</summary>
    public void Desactivar() => Activo = false;
}
