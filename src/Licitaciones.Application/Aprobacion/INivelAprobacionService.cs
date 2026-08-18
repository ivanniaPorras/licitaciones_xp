using Licitaciones.Application.Comun;

namespace Licitaciones.Application.Aprobacion;

/// <summary>Casos de uso del módulo de niveles de aprobación.</summary>
public interface INivelAprobacionService
{
    /// <summary>Determina quién debe aprobar el monto indicado.</summary>
    /// <param name="montoCRC">Monto que se quiere aprobar.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    Task<Result<NivelAprobacionResponse>> ObtenerAprobadorAsync(
        decimal montoCRC,
        CancellationToken cancelacion = default);

    /// <summary>Lista los niveles ordenados por su monto mínimo.</summary>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    Task<Result<IReadOnlyList<NivelAprobacionResponse>>> ListarAsync(
        CancellationToken cancelacion = default);

    /// <summary>Consulta un nivel por su identificador.</summary>
    /// <param name="id">Nivel consultado.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    Task<Result<NivelAprobacionResponse>> ObtenerAsync(Guid id, CancellationToken cancelacion = default);

    /// <summary>Registra un nivel nuevo, comprobando que no se traslape.</summary>
    /// <param name="peticion">Datos del nivel.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    Task<Result<NivelAprobacionResponse>> CrearAsync(
        CrearNivelAprobacionRequest peticion,
        CancellationToken cancelacion = default);

    /// <summary>Modifica un nivel, comprobando que siga sin traslaparse.</summary>
    /// <param name="id">Nivel que se modifica.</param>
    /// <param name="peticion">Nuevos datos.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    Task<Result<NivelAprobacionResponse>> ActualizarAsync(
        Guid id,
        ActualizarNivelAprobacionRequest peticion,
        CancellationToken cancelacion = default);

    /// <summary>Elimina un nivel.</summary>
    /// <param name="id">Nivel que se elimina.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    Task<Result> EliminarAsync(Guid id, CancellationToken cancelacion = default);
}
