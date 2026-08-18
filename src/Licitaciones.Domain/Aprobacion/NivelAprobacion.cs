using Licitaciones.Domain.Auditoria;
using Licitaciones.Domain.Dinero;

namespace Licitaciones.Domain.Aprobacion;

/// <summary>
/// Rango de montos con la instancia que tiene autoridad para aprobarlos. La política de
/// aprobación vive en esta tabla y no en el código, de modo que el cliente pueda
/// modificarla sin que el programa cambie.
/// </summary>
public sealed class NivelAprobacion : IAuditable
{
    // Constructor que Entity Framework Core usa al materializar la entidad.
    private NivelAprobacion() => Aprobador = string.Empty;

    private NivelAprobacion(MontoCRC montoMinimo, MontoCRC? montoMaximo, string aprobador)
    {
        MontoMinimo = montoMinimo;
        MontoMaximo = montoMaximo;
        Aprobador = aprobador;
    }

    /// <summary>Identificador generado por el sistema.</summary>
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>Monto a partir del cual aplica el nivel. Es inclusivo.</summary>
    public MontoCRC MontoMinimo { get; private set; }

    /// <summary>
    /// Monto hasta el que aplica el nivel, inclusivo. Es <c>null</c> cuando el rango no
    /// tiene límite superior.
    /// </summary>
    public MontoCRC? MontoMaximo { get; private set; }

    /// <summary>Instancia con autoridad para aprobar montos dentro del rango.</summary>
    public string Aprobador { get; private set; }

    /// <summary>Indica si el rango no tiene límite superior.</summary>
    public bool EsRangoAbierto => MontoMaximo is null;

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Crea un nivel de aprobación tras comprobar que su rango es coherente.</summary>
    /// <param name="montoMinimoCRC">Monto mínimo, inclusivo.</param>
    /// <param name="montoMaximoCRC">Monto máximo inclusivo, o <c>null</c> para un rango abierto.</param>
    /// <param name="aprobador">Instancia que aprueba los montos del rango.</param>
    /// <exception cref="MontoInvalidoException">Si algún monto no es mayor que cero.</exception>
    /// <exception cref="RangoAprobacionInvalidoException">
    /// Si el máximo es menor que el mínimo o si no se indica la persona aprobadora.
    /// </exception>
    public static NivelAprobacion Crear(decimal montoMinimoCRC, decimal? montoMaximoCRC, string aprobador)
    {
        if (string.IsNullOrWhiteSpace(aprobador))
        {
            throw new RangoAprobacionInvalidoException("El nivel de aprobación requiere una persona aprobadora.");
        }

        var minimo = MontoCRC.Crear(montoMinimoCRC);
        MontoCRC? maximo = montoMaximoCRC is null ? null : MontoCRC.Crear(montoMaximoCRC.Value);

        if (maximo is not null && maximo.Value < minimo)
        {
            throw new RangoAprobacionInvalidoException(
                "El monto máximo no puede ser menor que el monto mínimo.");
        }

        return new NivelAprobacion(minimo, maximo, aprobador.Trim());
    }

    /// <summary>Cambia los límites del rango y la persona aprobadora.</summary>
    /// <param name="montoMinimoCRC">Nuevo monto mínimo, inclusivo.</param>
    /// <param name="montoMaximoCRC">Nuevo monto máximo inclusivo, o <c>null</c> para un rango abierto.</param>
    /// <param name="aprobador">Nueva instancia aprobadora.</param>
    /// <exception cref="MontoInvalidoException">Si algún monto no es mayor que cero.</exception>
    /// <exception cref="RangoAprobacionInvalidoException">
    /// Si el máximo es menor que el mínimo o si no se indica la persona aprobadora.
    /// </exception>
    public void CambiarRango(decimal montoMinimoCRC, decimal? montoMaximoCRC, string aprobador)
    {
        var propuesto = Crear(montoMinimoCRC, montoMaximoCRC, aprobador);

        MontoMinimo = propuesto.MontoMinimo;
        MontoMaximo = propuesto.MontoMaximo;
        Aprobador = propuesto.Aprobador;
    }

    /// <summary>Indica si el monto cae dentro del rango, con ambos límites inclusivos.</summary>
    /// <param name="monto">Monto que se quiere aprobar.</param>
    public bool Cubre(MontoCRC monto) =>
        monto >= MontoMinimo && (EsRangoAbierto || monto <= MontoMaximo!.Value);

    /// <summary>
    /// Indica si este rango se solapa con otro. Dos rangos se traslapan cuando el mínimo
    /// de cada uno no supera al máximo del otro, tratando la ausencia de máximo como
    /// infinito.
    /// </summary>
    /// <param name="otro">Rango con el que se compara.</param>
    public bool SeTraslapaCon(NivelAprobacion otro)
    {
        var esteEmpiezaAntesDeQueTermineElOtro =
            otro.EsRangoAbierto || MontoMinimo <= otro.MontoMaximo!.Value;
        var elOtroEmpiezaAntesDeQueTermineEste =
            EsRangoAbierto || otro.MontoMinimo <= MontoMaximo!.Value;

        return esteEmpiezaAntesDeQueTermineElOtro && elOtroEmpiezaAntesDeQueTermineEste;
    }
}
