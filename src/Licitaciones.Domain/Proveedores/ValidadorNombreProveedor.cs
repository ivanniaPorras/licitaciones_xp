using System.Text.RegularExpressions;

namespace Licitaciones.Domain.Proveedores;

/// <summary>
/// Comprueba que el nombre de un proveedor use únicamente los caracteres admitidos:
/// letras, números, espacios, punto, coma y paréntesis.
/// </summary>
public static partial class ValidadorNombreProveedor
{
    /// <summary>Indica si el nombre usa solo caracteres admitidos y no está vacío.</summary>
    /// <param name="nombre">Nombre tal como lo escribió la persona usuaria.</param>
    public static bool EsValido(string? nombre) =>
        !string.IsNullOrWhiteSpace(nombre) && CaracteresAdmitidos().IsMatch(nombre);

    /// <summary>Comprueba el nombre y lo rechaza si usa caracteres no admitidos.</summary>
    /// <param name="nombre">Nombre tal como lo escribió la persona usuaria.</param>
    /// <exception cref="NombreProveedorInvalidoException">Si el nombre no es válido.</exception>
    public static void Validar(string? nombre)
    {
        if (!EsValido(nombre))
        {
            throw new NombreProveedorInvalidoException();
        }
    }

    // \p{L} cubre letras acentuadas y la eñe en cualquier idioma; restringir a ASCII
    // rechazaría nombres costarricenses perfectamente válidos.
    [GeneratedRegex(@"^[\p{L}\p{N} .,\(\)]+$")]
    private static partial Regex CaracteresAdmitidos();
}
