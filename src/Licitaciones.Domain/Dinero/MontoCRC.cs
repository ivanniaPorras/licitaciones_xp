namespace Licitaciones.Domain.Dinero;

/// <summary>
/// Monto expresado en colones costarricenses. Encapsula las dos reglas que todo valor
/// monetario del sistema debe cumplir: ser mayor que cero y no tener más de dos decimales.
/// </summary>
/// <remarks>
/// Se representa con <see cref="decimal"/> y nunca con coma flotante binaria, porque
/// <c>float</c> y <c>double</c> no pueden expresar exactamente cantidades como 0,01 y
/// acumulan error en las comparaciones de igualdad que exigen las reglas de negocio.
/// </remarks>
public readonly record struct MontoCRC : IComparable<MontoCRC>
{
    private const int DecimalesPermitidos = 2;

    private MontoCRC(decimal valor) => Valor = valor;

    /// <summary>Cantidad en colones.</summary>
    public decimal Valor { get; }

    /// <summary>Crea un monto tras comprobar que cumple las reglas monetarias.</summary>
    /// <param name="valor">Cantidad en colones.</param>
    /// <exception cref="MontoInvalidoException">
    /// Si el valor es cero o negativo, o si tiene más de dos decimales.
    /// </exception>
    public static MontoCRC Crear(decimal valor)
    {
        if (valor <= 0m)
        {
            throw new MontoInvalidoException("El monto debe ser mayor que cero.");
        }

        if (valor.Scale > DecimalesPermitidos)
        {
            throw new MontoInvalidoException("El monto no puede tener más de dos decimales.");
        }

        return new MontoCRC(valor);
    }

    /// <inheritdoc />
    public int CompareTo(MontoCRC otro) => Valor.CompareTo(otro.Valor);

    /// <summary>Indica si el monto de la izquierda es menor que el de la derecha.</summary>
    public static bool operator <(MontoCRC izquierda, MontoCRC derecha) =>
        izquierda.CompareTo(derecha) < 0;

    /// <summary>Indica si el monto de la izquierda es mayor que el de la derecha.</summary>
    public static bool operator >(MontoCRC izquierda, MontoCRC derecha) =>
        izquierda.CompareTo(derecha) > 0;

    /// <summary>Indica si el monto de la izquierda es menor o igual que el de la derecha.</summary>
    public static bool operator <=(MontoCRC izquierda, MontoCRC derecha) =>
        izquierda.CompareTo(derecha) <= 0;

    /// <summary>Indica si el monto de la izquierda es mayor o igual que el de la derecha.</summary>
    public static bool operator >=(MontoCRC izquierda, MontoCRC derecha) =>
        izquierda.CompareTo(derecha) >= 0;

    /// <inheritdoc />
    public override string ToString() => Valor.ToString("N2");
}
