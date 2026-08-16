using Licitaciones.Domain.Auditoria;

namespace Licitaciones.Domain.Proveedores;

/// <summary>
/// Empresa o persona que puede presentar ofertas. Se identifica por su nombre una vez
/// normalizado.
/// </summary>
public sealed class Proveedor : IAuditable, ISoftDeletable
{
    // Constructor que Entity Framework Core usa al materializar la entidad. Para crear un
    // proveedor desde el dominio está el método Crear, que aplica las validaciones.
    private Proveedor()
    {
        Nombre = string.Empty;
        NombreNormalizado = string.Empty;
    }

    private Proveedor(string nombre)
    {
        Nombre = nombre;
        NombreNormalizado = NormalizadorNombreProveedor.Normalizar(nombre);
    }

    /// <summary>Identificador generado por el sistema.</summary>
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>Nombre tal como lo escribió la persona usuaria.</summary>
    public string Nombre { get; private set; }

    /// <summary>Forma normalizada del nombre, que es la que se compara para detectar duplicados.</summary>
    public string NombreNormalizado { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? DeletedAt { get; private set; }

    /// <summary>Crea un proveedor tras comprobar que su nombre es válido.</summary>
    /// <param name="nombre">Nombre tal como lo escribió la persona usuaria.</param>
    /// <exception cref="NombreProveedorInvalidoException">
    /// Si el nombre está vacío o usa caracteres no admitidos.
    /// </exception>
    public static Proveedor Crear(string nombre)
    {
        ValidadorNombreProveedor.Validar(nombre);
        return new Proveedor(nombre);
    }

    /// <summary>Cambia el nombre del proveedor aplicando las mismas comprobaciones.</summary>
    /// <param name="nombre">Nuevo nombre.</param>
    /// <exception cref="NombreProveedorInvalidoException">
    /// Si el nombre está vacío o usa caracteres no admitidos.
    /// </exception>
    public void Renombrar(string nombre)
    {
        ValidadorNombreProveedor.Validar(nombre);
        Nombre = nombre;
        NombreNormalizado = NormalizadorNombreProveedor.Normalizar(nombre);
    }
}
