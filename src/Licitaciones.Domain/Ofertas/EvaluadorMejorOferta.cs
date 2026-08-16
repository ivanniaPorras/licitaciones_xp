namespace Licitaciones.Domain.Ofertas;

/// <summary>
/// Decide cuál es la mejor oferta de un conjunto: la de menor monto en colones.
/// </summary>
public static class EvaluadorMejorOferta
{
    /// <summary>
    /// Devuelve la oferta de menor monto, o <c>null</c> si el conjunto está vacío.
    /// </summary>
    /// <remarks>
    /// El orden de desempate es el acordado con el cliente: primero el monto, después el
    /// orden de llegada y, si ambas coincidieran, el identificador. Este último criterio
    /// no tiene significado de negocio; está para que el resultado sea siempre el mismo
    /// independientemente del orden en que la base de datos devuelva las filas.
    /// La fecha se compara en tiempo universal para que el huso horario con que se
    /// registró la oferta no altere quién llegó primero.
    /// </remarks>
    /// <param name="ofertas">Ofertas válidas de una licitación.</param>
    public static Oferta? Seleccionar(IEnumerable<Oferta> ofertas) =>
        ofertas
            .OrderBy(o => o.Monto.Valor)
            .ThenBy(o => o.FechaRegistro.ToUniversalTime())
            .ThenBy(o => o.Id)
            .FirstOrDefault();
}
