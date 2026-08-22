using Licitaciones.Application.Comun;

namespace Licitaciones.Application.Aprobacion;

/// <summary>Datos para registrar un nivel de aprobación.</summary>
/// <param name="MontoMinimoCRC">Monto mínimo, inclusivo.</param>
/// <param name="MontoMaximoCRC">Monto máximo inclusivo, o <c>null</c> para un rango abierto.</param>
/// <param name="Aprobador">Instancia que aprueba los montos del rango.</param>
public sealed record CrearNivelAprobacionRequest(
    decimal MontoMinimoCRC,
    decimal? MontoMaximoCRC,
    string Aprobador);

/// <summary>Datos para modificar un nivel de aprobación.</summary>
/// <param name="MontoMinimoCRC">Monto mínimo, inclusivo.</param>
/// <param name="MontoMaximoCRC">Monto máximo inclusivo, o <c>null</c> para un rango abierto.</param>
/// <param name="Aprobador">Instancia que aprueba los montos del rango.</param>
public sealed record ActualizarNivelAprobacionRequest(
    decimal MontoMinimoCRC,
    decimal? MontoMaximoCRC,
    string Aprobador);

/// <summary>Nivel de aprobación tal como se devuelve al exterior.</summary>
/// <param name="Id">Identificador generado por el sistema.</param>
/// <param name="MontoMinimoCRC">Monto mínimo, inclusivo.</param>
/// <param name="MontoMaximoCRC">Monto máximo inclusivo, o <c>null</c> si el rango es abierto.</param>
/// <param name="EsRangoAbierto">Indica si el rango no tiene límite superior.</param>
/// <param name="Aprobador">Instancia que aprueba los montos del rango.</param>
public sealed record NivelAprobacionResponse(
    Guid Id,
    decimal MontoMinimoCRC,
    decimal? MontoMaximoCRC,
    bool EsRangoAbierto,
    string Aprobador);

/// <summary>Filtros del listado de niveles de aprobación.</summary>
public sealed record ConsultaNivelesAprobacion : ConsultaPaginada
{
    /// <summary>Crea la consulta con los valores por omisión.</summary>
    public ConsultaNivelesAprobacion()
    {
    }

    /// <summary>Crea la consulta indicando sus parámetros.</summary>
    /// <param name="Pagina">Número de página.</param>
    /// <param name="Tamano">Elementos por página.</param>
    /// <param name="Orden">Campo y dirección de ordenamiento.</param>
    /// <param name="Busqueda">Término de búsqueda sobre el aprobador.</param>
    public ConsultaNivelesAprobacion(
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
