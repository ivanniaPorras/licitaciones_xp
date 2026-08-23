namespace Licitaciones.Web.Vistas;

/// <summary>Encabezado de tabla que ordena el listado al pulsarlo.</summary>
/// <param name="Campo">Campo por el que ordena, tal como lo entiende el servicio.</param>
/// <param name="Titulo">Texto visible del encabezado.</param>
/// <param name="Accion">Acción del controlador que atiende el listado.</param>
/// <param name="DireccionInicial">Dirección que se pide la primera vez que se pulsa.</param>
public sealed record ColumnaOrdenable(
    string Campo,
    string Titulo,
    string Accion = "Index",
    string DireccionInicial = "asc");
