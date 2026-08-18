using Licitaciones.Application.Comun;
using Licitaciones.Application.Ofertas;

namespace Licitaciones.Application.Proveedores;

/// <summary>Casos de uso del módulo de proveedores.</summary>
public interface IProveedorService
{
    /// <summary>Registra un proveedor nuevo.</summary>
    /// <param name="peticion">Datos del proveedor.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    Task<Result<ProveedorResponse>> CrearAsync(
        CrearProveedorRequest peticion,
        CancellationToken cancelacion = default);

    /// <summary>Modifica el nombre de un proveedor.</summary>
    /// <param name="id">Proveedor que se modifica.</param>
    /// <param name="peticion">Nuevos datos.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    Task<Result<ProveedorResponse>> ActualizarAsync(
        Guid id,
        ActualizarProveedorRequest peticion,
        CancellationToken cancelacion = default);

    /// <summary>Consulta un proveedor por su identificador.</summary>
    /// <param name="id">Proveedor consultado.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    Task<Result<ProveedorResponse>> ObtenerAsync(Guid id, CancellationToken cancelacion = default);

    /// <summary>Lista los proveedores con paginación, filtrado y ordenamiento.</summary>
    /// <param name="consulta">Filtros del listado.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    Task<Result<PagedResponse<ProveedorResponse>>> ListarAsync(
        ConsultaProveedores consulta,
        CancellationToken cancelacion = default);

    /// <summary>Da de baja un proveedor. El borrado es lógico.</summary>
    /// <param name="id">Proveedor que se da de baja.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    Task<Result> EliminarAsync(Guid id, CancellationToken cancelacion = default);

    /// <summary>Consulta las ofertas que ha presentado un proveedor.</summary>
    /// <param name="id">Proveedor consultado.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    Task<Result<IReadOnlyList<OfertaResponse>>> ObtenerOfertasAsync(
        Guid id,
        CancellationToken cancelacion = default);
}
