using Licitaciones.Application.Comun;

namespace Licitaciones.Application.Ofertas;

/// <summary>Casos de uso del módulo de ofertas.</summary>
public interface IOfertaService
{
    /// <summary>Registra una oferta sobre una licitación publicada y vigente.</summary>
    /// <param name="peticion">Datos de la oferta.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    Task<Result<OfertaResponse>> CrearAsync(
        CrearOfertaRequest peticion,
        CancellationToken cancelacion = default);

    /// <summary>Modifica el monto de una oferta, si su licitación sigue vigente.</summary>
    /// <param name="id">Oferta que se modifica.</param>
    /// <param name="peticion">Nuevo monto.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    Task<Result<OfertaResponse>> ActualizarAsync(
        Guid id,
        ActualizarOfertaRequest peticion,
        CancellationToken cancelacion = default);

    /// <summary>Elimina una oferta, si su licitación sigue vigente.</summary>
    /// <param name="id">Oferta que se elimina.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    Task<Result> EliminarAsync(Guid id, CancellationToken cancelacion = default);

    /// <summary>Consulta una oferta por su identificador.</summary>
    /// <param name="id">Oferta consultada.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    Task<Result<OfertaResponse>> ObtenerAsync(Guid id, CancellationToken cancelacion = default);

    /// <summary>Lista las ofertas con filtro por licitación y por proveedor.</summary>
    /// <param name="consulta">Filtros del listado.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    Task<Result<PagedResponse<OfertaResponse>>> ListarAsync(
        ConsultaOfertas consulta,
        CancellationToken cancelacion = default);
}
