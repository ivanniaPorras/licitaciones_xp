namespace Licitaciones.Domain.Licitaciones;

/// <summary>
/// Produce la forma normalizada del código de una licitación, que es la que se compara
/// para decidir si dos códigos son el mismo.
/// </summary>
public static class NormalizadorCodigo
{
    /// <summary>
    /// Recorta los espacios de los extremos y pasa el código a mayúsculas. Se usa
    /// <c>ToUpperInvariant</c> y no la cultura actual para que el resultado no dependa de
    /// la configuración regional de la máquina.
    /// </summary>
    /// <param name="codigo">Código tal como lo escribió la persona usuaria.</param>
    /// <exception cref="ArgumentNullException">Si el código es nulo.</exception>
    /// <exception cref="ArgumentException">Si el código está vacío o solo tiene espacios.</exception>
    public static string Normalizar(string codigo)
    {
        ArgumentNullException.ThrowIfNull(codigo);

        var recortado = codigo.Trim();
        if (recortado.Length == 0)
        {
            throw new ArgumentException("El código de la licitación es obligatorio.", nameof(codigo));
        }

        return recortado.ToUpperInvariant();
    }
}
