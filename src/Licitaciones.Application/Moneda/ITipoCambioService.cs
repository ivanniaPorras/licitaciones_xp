using Licitaciones.Application.Comun;

namespace Licitaciones.Application.Moneda;

/// <summary>Casos de uso del módulo de tipo de cambio.</summary>
public interface ITipoCambioService
{
    /// <summary>Lista las tasas registradas, de la más reciente a la más antigua.</summary>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    Task<Result<IReadOnlyList<TipoCambioResponse>>> ListarAsync(CancellationToken cancelacion = default);

    /// <summary>Consulta una tasa por su identificador.</summary>
    /// <param name="id">Tasa consultada.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    Task<Result<TipoCambioResponse>> ObtenerAsync(Guid id, CancellationToken cancelacion = default);

    /// <summary>Devuelve la tasa que el sistema está usando para convertir.</summary>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    Task<Result<TipoCambioResponse>> ObtenerVigenteAsync(CancellationToken cancelacion = default);

    /// <summary>Registra una tasa nueva, que nace sin estar en uso.</summary>
    /// <param name="peticion">Datos de la tasa.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    Task<Result<TipoCambioResponse>> CrearAsync(
        CrearTipoCambioRequest peticion,
        CancellationToken cancelacion = default);

    /// <summary>Modifica la tasa y su fecha de vigencia.</summary>
    /// <param name="id">Tasa que se modifica.</param>
    /// <param name="peticion">Nuevos datos.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    Task<Result<TipoCambioResponse>> ActualizarAsync(
        Guid id,
        ActualizarTipoCambioRequest peticion,
        CancellationToken cancelacion = default);

    /// <summary>Pone la tasa en uso y retira de uso a la que lo estuviera.</summary>
    /// <param name="id">Tasa que pasa a estar vigente.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    Task<Result<TipoCambioResponse>> ActivarAsync(Guid id, CancellationToken cancelacion = default);

    /// <summary>Elimina una tasa.</summary>
    /// <param name="id">Tasa que se elimina.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    Task<Result> EliminarAsync(Guid id, CancellationToken cancelacion = default);
}
