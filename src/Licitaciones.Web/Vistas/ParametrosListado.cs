using Microsoft.AspNetCore.Http;

namespace Licitaciones.Web.Vistas;

/// <summary>
/// Construye los enlaces de un listado conservando los filtros que ya están puestos.
/// </summary>
/// <remarks>
/// Sin esto, pasar a la página siguiente o cambiar el ordenamiento perdería la búsqueda y
/// los filtros, porque cada enlace solo llevaría su propio parámetro. La persona usuaria
/// vería la lista completa justo después de haberla filtrado.
/// </remarks>
public static class ParametrosListado
{
    private const string ClaveOrden = "orden";
    private const string ClavePagina = "pagina";

    /// <summary>Enlace que cambia el ordenamiento y vuelve a la primera página.</summary>
    /// <param name="peticion">Petición en curso, de la que se toman los filtros vigentes.</param>
    /// <param name="orden">Campo y dirección de destino.</param>
    public static Dictionary<string, string> ConOrden(HttpRequest peticion, string orden)
    {
        // Cambiar el orden vuelve a la primera página: quedarse en la página siete de un
        // listado recién reordenado no muestra nada que la persona usuaria estuviera viendo.
        var parametros = Actuales(peticion);
        parametros[ClaveOrden] = orden;
        parametros.Remove(ClavePagina);

        return parametros;
    }

    /// <summary>Enlace que cambia de página y conserva búsqueda, filtros y ordenamiento.</summary>
    /// <param name="peticion">Petición en curso, de la que se toman los filtros vigentes.</param>
    /// <param name="pagina">Página de destino.</param>
    public static Dictionary<string, string> ConPagina(HttpRequest peticion, int pagina)
    {
        var parametros = Actuales(peticion);
        parametros[ClavePagina] = pagina.ToString(System.Globalization.CultureInfo.InvariantCulture);

        return parametros;
    }

    /// <summary>
    /// Resuelve cómo debe pintarse una columna ordenable: si es la que ordena el listado,
    /// hacia dónde apunta y qué orden pedirá al pulsarla.
    /// </summary>
    /// <param name="ordenActual">Valor del parámetro <c>orden</c> en la petición.</param>
    /// <param name="campo">Campo que representa la columna.</param>
    /// <param name="direccionInicial">Dirección que se pide la primera vez que se pulsa.</param>
    public static EstadoColumna EstadoDe(string? ordenActual, string campo, string direccionInicial)
    {
        var activa = !string.IsNullOrEmpty(ordenActual)
            && ordenActual.StartsWith(campo + ":", StringComparison.Ordinal);

        if (!activa)
        {
            return new EstadoColumna(false, $"{campo}:{direccionInicial}", string.Empty, "none");
        }

        var ascendente = ordenActual!.EndsWith(":asc", StringComparison.Ordinal);
        var contraria = ascendente ? "desc" : "asc";

        return new EstadoColumna(
            true,
            $"{campo}:{contraria}",
            ascendente ? "▲" : "▼",
            ascendente ? "ascending" : "descending");
    }

    private static Dictionary<string, string> Actuales(HttpRequest peticion)
    {
        ArgumentNullException.ThrowIfNull(peticion);

        var parametros = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var parametro in peticion.Query)
        {
            var valor = parametro.Value.ToString();
            if (!string.IsNullOrEmpty(valor))
            {
                parametros[parametro.Key] = valor;
            }
        }

        return parametros;
    }
}

/// <summary>Cómo debe pintarse una columna ordenable.</summary>
/// <param name="Activa">Indica si el listado está ordenado por esta columna.</param>
/// <param name="OrdenDestino">Orden que se pedirá al pulsar el encabezado.</param>
/// <param name="Indicador">Flecha que se muestra, o cadena vacía si la columna no ordena.</param>
/// <param name="AriaSort">Valor del atributo <c>aria-sort</c> del encabezado.</param>
public sealed record EstadoColumna(bool Activa, string OrdenDestino, string Indicador, string AriaSort);
