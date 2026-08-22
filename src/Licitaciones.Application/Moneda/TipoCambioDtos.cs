using Licitaciones.Application.Comun;

namespace Licitaciones.Application.Moneda;

/// <summary>Datos para registrar un tipo de cambio.</summary>
/// <param name="CRCporUSD">Colones equivalentes a un dólar.</param>
/// <param name="FechaVigencia">Fecha desde la que rige la tasa.</param>
public sealed record CrearTipoCambioRequest(decimal CRCporUSD, DateTimeOffset FechaVigencia);

/// <summary>Datos para modificar un tipo de cambio.</summary>
/// <param name="CRCporUSD">Colones equivalentes a un dólar.</param>
/// <param name="FechaVigencia">Fecha desde la que rige la tasa.</param>
public sealed record ActualizarTipoCambioRequest(decimal CRCporUSD, DateTimeOffset FechaVigencia);

/// <summary>Tipo de cambio tal como se devuelve al exterior.</summary>
/// <param name="Id">Identificador generado por el sistema.</param>
/// <param name="CRCporUSD">Colones equivalentes a un dólar.</param>
/// <param name="FechaVigencia">Fecha desde la que rige la tasa.</param>
/// <param name="Activo">Indica si es la tasa que el sistema usa para convertir.</param>
/// <param name="CreatedAt">Instante de registro.</param>
/// <param name="UpdatedAt">Instante de la última modificación.</param>
public sealed record TipoCambioResponse(
    Guid Id,
    decimal CRCporUSD,
    DateTimeOffset FechaVigencia,
    bool Activo,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>
/// Monto expresado en las dos monedas, acompañado siempre de la tasa con la que se
/// calculó y de su fecha de vigencia.
/// </summary>
/// <param name="MontoCRC">Monto en colones, que es el valor realmente almacenado.</param>
/// <param name="MontoUSD">Equivalente en dólares, redondeado a dos decimales.</param>
/// <param name="CRCporUSD">Tasa usada en la conversión.</param>
/// <param name="FechaVigencia">Fecha desde la que rige esa tasa.</param>
public sealed record ConversionResponse(
    decimal MontoCRC,
    decimal MontoUSD,
    decimal CRCporUSD,
    DateTimeOffset FechaVigencia);

/// <summary>Filtros del listado de tipos de cambio.</summary>
public sealed record ConsultaTiposCambio : ConsultaPaginada
{
    /// <summary>Crea la consulta con los valores por omisión.</summary>
    public ConsultaTiposCambio()
    {
    }

    /// <summary>Crea la consulta indicando sus parámetros.</summary>
    /// <param name="Pagina">Número de página.</param>
    /// <param name="Tamano">Elementos por página.</param>
    /// <param name="Orden">Campo y dirección de ordenamiento.</param>
    /// <param name="Busqueda">Año de vigencia por el que se filtra.</param>
    public ConsultaTiposCambio(
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
