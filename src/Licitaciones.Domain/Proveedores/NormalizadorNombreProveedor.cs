using System.Text;
using System.Text.RegularExpressions;

namespace Licitaciones.Domain.Proveedores;

/// <summary>
/// Produce la forma normalizada del nombre de un proveedor, que es la que se compara para
/// decidir si dos nombres designan a la misma empresa.
/// </summary>
public static partial class NormalizadorNombreProveedor
{
    /// <summary>
    /// Aplica, en este orden: normalización Unicode a forma compuesta de compatibilidad,
    /// recorte de los extremos, colapso de espacios repetidos y paso a minúsculas.
    /// </summary>
    /// <remarks>
    /// La normalización Unicode va primero porque unifica letras escritas como carácter
    /// precompuesto o como letra base más marca combinante, y convierte separadores de
    /// compatibilidad —como el espacio duro— en el espacio ordinario que el paso siguiente
    /// sabe colapsar.
    /// </remarks>
    /// <param name="nombre">Nombre tal como lo escribió la persona usuaria.</param>
    /// <exception cref="ArgumentNullException">Si el nombre es nulo.</exception>
    /// <exception cref="ArgumentException">Si el nombre está vacío o solo tiene espacios.</exception>
    public static string Normalizar(string nombre)
    {
        ArgumentNullException.ThrowIfNull(nombre);

        var unificado = nombre.Normalize(NormalizationForm.FormKC).Trim();
        var sinEspaciosRepetidos = EspaciosRepetidos().Replace(unificado, " ").Trim();

        if (sinEspaciosRepetidos.Length == 0)
        {
            throw new ArgumentException("El nombre del proveedor es obligatorio.", nameof(nombre));
        }

        return sinEspaciosRepetidos.ToLowerInvariant();
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex EspaciosRepetidos();
}
