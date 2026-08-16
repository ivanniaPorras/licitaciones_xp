namespace Licitaciones.Domain.Ofertas;

/// <summary>
/// Resultado de evaluar qué tan conveniente es la mejor oferta de una licitación.
/// </summary>
/// <param name="Etiqueta">
/// Texto de clasificación acordado con el cliente. Es uno de estos cuatro valores exactos:
/// "Sin ofertas válidas", "Oferta conveniente", "Oferta aceptable" u
/// "Oferta válida sin ahorro".
/// </param>
/// <param name="PorcentajeAhorro">
/// Ahorro respecto del presupuesto, redondeado a dos decimales. Es <c>null</c> cuando no
/// hay ofertas válidas, porque en ese caso no hay nada que comparar.
/// </param>
public readonly record struct ResultadoClasificacion(string Etiqueta, decimal? PorcentajeAhorro);
