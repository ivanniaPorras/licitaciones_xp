using Licitaciones.Domain.Auditoria;
using Licitaciones.Domain.Dinero;

namespace Licitaciones.Domain.Ofertas;

/// <summary>
/// Propuesta económica que un proveedor presenta a una licitación. Un proveedor presenta
/// a lo sumo una oferta por licitación.
/// </summary>
/// <remarks>
/// La oferta no admite borrado lógico: o existe, o se elimina físicamente mientras la
/// licitación siga vigente. Una vez cerrada la licitación queda inmutable, y conservarla
/// es precisamente lo que da valor probatorio al proceso.
/// </remarks>
public sealed class Oferta : IAuditable
{
    // Constructor que Entity Framework Core usa al materializar la entidad.
    private Oferta()
    {
    }

    private Oferta(Guid licitacionId, Guid proveedorId, MontoCRC monto, DateTimeOffset fechaRegistro)
    {
        LicitacionId = licitacionId;
        ProveedorId = proveedorId;
        Monto = monto;
        FechaRegistro = fechaRegistro;
    }

    /// <summary>Identificador generado por el sistema.</summary>
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>Licitación a la que se presenta la oferta.</summary>
    public Guid LicitacionId { get; private set; }

    /// <summary>Proveedor que presenta la oferta.</summary>
    public Guid ProveedorId { get; private set; }

    /// <summary>Monto propuesto, en colones.</summary>
    public MontoCRC Monto { get; private set; }

    /// <summary>
    /// Instante en que se recibió la oferta. Define el orden de llegada y, con ello, el
    /// desempate cuando dos ofertas coinciden en monto.
    /// </summary>
    public DateTimeOffset FechaRegistro { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Crea una oferta tras comprobar que su monto cumple las reglas monetarias.</summary>
    /// <param name="licitacionId">Licitación a la que se presenta.</param>
    /// <param name="proveedorId">Proveedor que la presenta.</param>
    /// <param name="montoOfertadoCRC">Monto propuesto en colones.</param>
    /// <param name="fechaRegistro">Instante de recepción.</param>
    public static Oferta Crear(
        Guid licitacionId,
        Guid proveedorId,
        decimal montoOfertadoCRC,
        DateTimeOffset fechaRegistro) =>
        new(licitacionId, proveedorId, MontoCRC.Crear(montoOfertadoCRC), fechaRegistro);

    /// <summary>Cambia el monto ofertado aplicando las reglas monetarias.</summary>
    /// <param name="montoOfertadoCRC">Nuevo monto en colones.</param>
    public void CambiarMonto(decimal montoOfertadoCRC) => Monto = MontoCRC.Crear(montoOfertadoCRC);
}
