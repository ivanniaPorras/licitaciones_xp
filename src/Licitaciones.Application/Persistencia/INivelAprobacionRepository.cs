using Licitaciones.Domain.Aprobacion;
using Licitaciones.Domain.Dinero;

namespace Licitaciones.Application.Persistencia;

/// <summary>Acceso a los niveles de aprobación almacenados.</summary>
public interface INivelAprobacionRepository
{
    /// <summary>Busca un nivel por su identificador.</summary>
    /// <param name="id">Identificador del nivel.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    Task<NivelAprobacion?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion = default);

    /// <summary>Devuelve todos los niveles ordenados por su monto mínimo.</summary>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    Task<IReadOnlyList<NivelAprobacion>> ObtenerTodosAsync(CancellationToken cancelacion = default);

    /// <summary>
    /// Devuelve el nivel aplicable al monto, consultando la tabla. La política de
    /// aprobación no se resuelve con condicionales escritos en el código.
    /// </summary>
    /// <param name="monto">Monto que se quiere aprobar.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    Task<NivelAprobacion?> ObtenerAplicableAsync(MontoCRC monto, CancellationToken cancelacion = default);

    /// <summary>Lista los niveles con paginación, filtrado y ordenamiento.</summary>
    /// <param name="busqueda">Término de búsqueda sobre el aprobador, o <c>null</c>.</param>
    /// <param name="orden">Campo y dirección de ordenamiento, o <c>null</c> para el orden natural.</param>
    /// <param name="pagina">Número de página, empezando en 1.</param>
    /// <param name="tamano">Elementos por página.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    Task<(IReadOnlyList<NivelAprobacion> Elementos, int Total)> ListarAsync(
        string? busqueda,
        string? orden,
        int pagina,
        int tamano,
        CancellationToken cancelacion = default);

    /// <summary>Agrega un nivel nuevo.</summary>
    /// <param name="nivel">Nivel que se va a guardar.</param>
    void Agregar(NivelAprobacion nivel);

    /// <summary>Elimina el nivel.</summary>
    /// <param name="nivel">Nivel que se elimina.</param>
    void Eliminar(NivelAprobacion nivel);
}
