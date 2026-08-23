using Licitaciones.Domain.Dinero;

namespace Licitaciones.Application.Persistencia;

/// <summary>Acceso a los tipos de cambio almacenados.</summary>
public interface ITipoCambioRepository
{
    /// <summary>Busca un tipo de cambio por su identificador.</summary>
    /// <param name="id">Identificador del tipo de cambio.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    Task<TipoCambio?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion = default);

    /// <summary>Devuelve el tipo de cambio vigente, o <c>null</c> si no hay ninguno activo.</summary>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    Task<TipoCambio?> ObtenerActivoAsync(CancellationToken cancelacion = default);

    /// <summary>Lista los tipos de cambio con paginación, filtrado y ordenamiento.</summary>
    /// <param name="anioVigencia">Año de vigencia por el que se filtra, o <c>null</c>.</param>
    /// <param name="orden">Campo y dirección de ordenamiento, o <c>null</c> para el orden natural.</param>
    /// <param name="pagina">Número de página, empezando en 1.</param>
    /// <param name="tamano">Elementos por página.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    Task<(IReadOnlyList<TipoCambio> Elementos, int Total)> ListarAsync(
        int? anioVigencia,
        string? orden,
        int pagina,
        int tamano,
        CancellationToken cancelacion = default);

    /// <summary>Agrega un tipo de cambio nuevo.</summary>
    /// <param name="tipoCambio">Tipo de cambio que se va a guardar.</param>
    void Agregar(TipoCambio tipoCambio);

    /// <summary>Elimina el tipo de cambio.</summary>
    /// <param name="tipoCambio">Tipo de cambio que se elimina.</param>
    void Eliminar(TipoCambio tipoCambio);
}
