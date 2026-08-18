using Licitaciones.Application.Ofertas;
using Licitaciones.Domain.Dinero;
using Licitaciones.Domain.Ofertas;

namespace Licitaciones.Application.Persistencia;

/// <summary>Acceso a las ofertas almacenadas.</summary>
public interface IOfertaRepository
{
    /// <summary>Busca una oferta por su identificador.</summary>
    /// <param name="id">Identificador de la oferta.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    Task<Oferta?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion = default);

    /// <summary>Devuelve las ofertas presentadas a una licitación.</summary>
    /// <param name="licitacionId">Licitación consultada.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    Task<IReadOnlyList<Oferta>> ObtenerPorLicitacionAsync(
        Guid licitacionId,
        CancellationToken cancelacion = default);

    /// <summary>Devuelve las ofertas presentadas por un proveedor.</summary>
    /// <param name="proveedorId">Proveedor consultado.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    Task<IReadOnlyList<Oferta>> ObtenerPorProveedorAsync(
        Guid proveedorId,
        CancellationToken cancelacion = default);

    /// <summary>Indica si el proveedor ya presentó una oferta a esa licitación.</summary>
    /// <param name="licitacionId">Licitación consultada.</param>
    /// <param name="proveedorId">Proveedor consultado.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    Task<bool> ExisteOfertaDelProveedorAsync(
        Guid licitacionId,
        Guid proveedorId,
        CancellationToken cancelacion = default);

    /// <summary>
    /// Devuelve el monto de la oferta más alta registrada, o <c>null</c> si la licitación
    /// no tiene ofertas. Lo usa la regla que impide bajar el presupuesto por debajo de una
    /// oferta ya recibida.
    /// </summary>
    /// <param name="licitacionId">Licitación consultada.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    Task<MontoCRC?> ObtenerMontoMaximoAsync(Guid licitacionId, CancellationToken cancelacion = default);

    /// <summary>Indica si existe alguna oferta asociada a la licitación.</summary>
    /// <param name="licitacionId">Licitación consultada.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    Task<bool> TieneOfertasLaLicitacionAsync(Guid licitacionId, CancellationToken cancelacion = default);

    /// <summary>Indica si existe alguna oferta presentada por el proveedor.</summary>
    /// <param name="proveedorId">Proveedor consultado.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    Task<bool> TieneOfertasElProveedorAsync(Guid proveedorId, CancellationToken cancelacion = default);

    /// <summary>
    /// Lista las ofertas ya combinadas con el código de su licitación y el nombre de su
    /// proveedor. La combinación se resuelve en una sola consulta, en lugar de pedirle a
    /// cada módulo su parte y unirlas en memoria.
    /// </summary>
    /// <param name="consulta">Filtros del listado.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    Task<(IReadOnlyList<OfertaResponse> Elementos, int Total)> ListarDetalleAsync(
        ConsultaOfertas consulta,
        CancellationToken cancelacion = default);

    /// <summary>Consulta una oferta con el detalle de su licitación y su proveedor.</summary>
    /// <param name="id">Oferta consultada.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    Task<OfertaResponse?> ObtenerDetalleAsync(Guid id, CancellationToken cancelacion = default);

    /// <summary>Agrega una oferta nueva.</summary>
    /// <param name="oferta">Oferta que se va a guardar.</param>
    void Agregar(Oferta oferta);

    /// <summary>Elimina la oferta. A diferencia de las demás entidades, el borrado es físico.</summary>
    /// <param name="oferta">Oferta que se elimina.</param>
    void Eliminar(Oferta oferta);
}
