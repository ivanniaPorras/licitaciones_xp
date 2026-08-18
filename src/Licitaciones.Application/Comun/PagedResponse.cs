namespace Licitaciones.Application.Comun;

/// <summary>Página de resultados de un listado.</summary>
/// <typeparam name="T">Tipo de los elementos.</typeparam>
/// <param name="Elementos">Elementos de la página solicitada.</param>
/// <param name="Pagina">Número de página, empezando en 1.</param>
/// <param name="Tamano">Cantidad de elementos por página.</param>
/// <param name="Total">Cantidad total de elementos que cumplen el filtro.</param>
public sealed record PagedResponse<T>(IReadOnlyList<T> Elementos, int Pagina, int Tamano, int Total)
{
    /// <summary>Cantidad de páginas disponibles con el tamaño actual.</summary>
    public int TotalPaginas => Tamano <= 0 ? 0 : (int)Math.Ceiling(Total / (double)Tamano);
}
