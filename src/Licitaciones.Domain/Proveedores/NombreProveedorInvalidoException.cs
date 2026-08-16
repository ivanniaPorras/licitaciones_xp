using Licitaciones.Domain.Excepciones;

namespace Licitaciones.Domain.Proveedores;

/// <summary>
/// Se produce cuando el nombre de un proveedor contiene caracteres no admitidos.
/// </summary>
public sealed class NombreProveedorInvalidoException : ReglaNegocioException
{
    /// <summary>Crea la excepción con el mensaje acordado con el cliente.</summary>
    public NombreProveedorInvalidoException()
        : base("El nombre solo admite letras, números, espacios, punto, coma y paréntesis.")
    {
    }
}
