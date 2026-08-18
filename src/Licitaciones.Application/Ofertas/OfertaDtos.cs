using Licitaciones.Application.Comun;

namespace Licitaciones.Application.Ofertas;

/// <summary>Datos para registrar una oferta.</summary>
/// <param name="LicitacionId">Licitación a la que se presenta.</param>
/// <param name="ProveedorId">Proveedor que la presenta.</param>
/// <param name="MontoOfertadoCRC">Monto propuesto en colones.</param>
public sealed record CrearOfertaRequest(Guid LicitacionId, Guid ProveedorId, decimal MontoOfertadoCRC);

/// <summary>Datos para modificar el monto de una oferta.</summary>
/// <param name="MontoOfertadoCRC">Nuevo monto en colones.</param>
public sealed record ActualizarOfertaRequest(decimal MontoOfertadoCRC);

/// <summary>Oferta tal como se devuelve al exterior.</summary>
/// <param name="Id">Identificador generado por el sistema.</param>
/// <param name="LicitacionId">Licitación a la que pertenece.</param>
/// <param name="CodigoLicitacion">Código de esa licitación.</param>
/// <param name="ProveedorId">Proveedor que la presentó.</param>
/// <param name="NombreProveedor">Nombre de ese proveedor.</param>
/// <param name="MontoOfertadoCRC">Monto propuesto en colones.</param>
/// <param name="FechaRegistro">Instante en que se recibió.</param>
public sealed record OfertaResponse(
    Guid Id,
    Guid LicitacionId,
    string CodigoLicitacion,
    Guid ProveedorId,
    string NombreProveedor,
    decimal MontoOfertadoCRC,
    DateTimeOffset FechaRegistro);

/// <summary>Filtros del listado de ofertas.</summary>
public sealed record ConsultaOfertas : ConsultaPaginada
{
    /// <summary>Crea la consulta con los valores por omisión.</summary>
    public ConsultaOfertas()
    {
    }

    /// <summary>Licitación por la que se filtra, si se indica.</summary>
    public Guid? LicitacionId { get; init; }

    /// <summary>Proveedor por el que se filtra, si se indica.</summary>
    public Guid? ProveedorId { get; init; }
}
