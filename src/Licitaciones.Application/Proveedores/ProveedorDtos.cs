using Licitaciones.Application.Comun;

namespace Licitaciones.Application.Proveedores;

/// <summary>Datos para registrar un proveedor.</summary>
/// <param name="Nombre">Nombre tal como lo escribe la persona usuaria.</param>
public sealed record CrearProveedorRequest(string Nombre);

/// <summary>Datos para modificar un proveedor.</summary>
/// <param name="Nombre">Nuevo nombre.</param>
public sealed record ActualizarProveedorRequest(string Nombre);

/// <summary>Proveedor tal como se devuelve al exterior.</summary>
/// <param name="Id">Identificador generado por el sistema.</param>
/// <param name="Nombre">Nombre tal como se registró.</param>
/// <param name="CantidadOfertas">Ofertas que ha presentado.</param>
/// <param name="CreatedAt">Instante de registro.</param>
/// <param name="UpdatedAt">Instante de la última modificación.</param>
public sealed record ProveedorResponse(
    Guid Id,
    string Nombre,
    int CantidadOfertas,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>Filtros del listado de proveedores.</summary>
public sealed record ConsultaProveedores : ConsultaPaginada
{
    /// <summary>Crea la consulta con los valores por omisión.</summary>
    public ConsultaProveedores()
    {
    }

    /// <summary>Crea la consulta indicando sus parámetros.</summary>
    /// <param name="Pagina">Número de página.</param>
    /// <param name="Tamano">Elementos por página.</param>
    /// <param name="Orden">Campo y dirección de ordenamiento.</param>
    /// <param name="Busqueda">Término de búsqueda.</param>
    public ConsultaProveedores(
        int Pagina = 1,
        int Tamano = TamanoPorDefecto,
        string? Orden = null,
        string? Busqueda = null)
    {
        this.Pagina = Pagina;
        this.Tamano = Tamano;
        this.Orden = Orden;
        this.Busqueda = Busqueda;
    }
}
