using Licitaciones.Application.Comun;
using Licitaciones.Application.Ofertas;

namespace Licitaciones.Application.Licitaciones;

/// <summary>Casos de uso del módulo de licitaciones.</summary>
public interface ILicitacionService
{
    /// <summary>Registra una licitación nueva, siempre en estado Borrador.</summary>
    /// <param name="peticion">Datos de la licitación.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    Task<Result<LicitacionResponse>> CrearAsync(
        CrearLicitacionRequest peticion,
        CancellationToken cancelacion = default);

    /// <summary>Modifica los datos de una licitación.</summary>
    /// <param name="id">Licitación que se modifica.</param>
    /// <param name="peticion">Nuevos datos.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    Task<Result<LicitacionResponse>> ActualizarAsync(
        Guid id,
        ActualizarLicitacionRequest peticion,
        CancellationToken cancelacion = default);

    /// <summary>Consulta una licitación por su identificador.</summary>
    /// <param name="id">Licitación consultada.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    Task<Result<LicitacionResponse>> ObtenerAsync(Guid id, CancellationToken cancelacion = default);

    /// <summary>Lista las licitaciones con paginación, filtrado y ordenamiento.</summary>
    /// <param name="consulta">Filtros del listado.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    Task<Result<PagedResponse<LicitacionResponse>>> ListarAsync(
        ConsultaLicitaciones consulta,
        CancellationToken cancelacion = default);

    /// <summary>Da de baja una licitación. El borrado es lógico.</summary>
    /// <param name="id">Licitación que se da de baja.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    Task<Result> EliminarAsync(Guid id, CancellationToken cancelacion = default);

    /// <summary>Lleva la licitación al estado indicado si la transición está permitida.</summary>
    /// <param name="id">Licitación que cambia de estado.</param>
    /// <param name="peticion">Estado destino.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    Task<Result<LicitacionResponse>> CambiarEstadoAsync(
        Guid id,
        CambiarEstadoRequest peticion,
        CancellationToken cancelacion = default);

    /// <summary>Consulta las ofertas recibidas por una licitación.</summary>
    /// <param name="id">Licitación consultada.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    Task<Result<IReadOnlyList<OfertaResponse>>> ObtenerOfertasAsync(
        Guid id,
        CancellationToken cancelacion = default);

    /// <summary>Consulta la mejor oferta con su ahorro y su clasificación.</summary>
    /// <param name="id">Licitación consultada.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    Task<Result<MejorOfertaResponse>> ObtenerMejorOfertaAsync(
        Guid id,
        CancellationToken cancelacion = default);
}
