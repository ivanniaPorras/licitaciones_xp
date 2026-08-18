using Licitaciones.Application.Comun;
using Licitaciones.Application.Ofertas;
using Licitaciones.Domain.Licitaciones;

namespace Licitaciones.Application.Licitaciones;

/// <summary>Datos para registrar una licitación.</summary>
/// <param name="Codigo">Código identificador del proceso.</param>
/// <param name="Titulo">Título descriptivo.</param>
/// <param name="PresupuestoEstimadoCRC">Presupuesto estimado en colones.</param>
/// <param name="FechaCierre">Instante de cierre de la recepción de ofertas.</param>
public sealed record CrearLicitacionRequest(
    string Codigo,
    string Titulo,
    decimal PresupuestoEstimadoCRC,
    DateTimeOffset FechaCierre);

/// <summary>Datos para modificar una licitación.</summary>
/// <param name="Codigo">Código identificador del proceso.</param>
/// <param name="Titulo">Título descriptivo.</param>
/// <param name="PresupuestoEstimadoCRC">Presupuesto estimado en colones.</param>
/// <param name="FechaCierre">Instante de cierre de la recepción de ofertas.</param>
public sealed record ActualizarLicitacionRequest(
    string Codigo,
    string Titulo,
    decimal PresupuestoEstimadoCRC,
    DateTimeOffset FechaCierre);

/// <summary>Estado al que se quiere llevar una licitación.</summary>
/// <param name="Estado">Estado destino.</param>
/// <param name="Motivo">Motivo de la cancelación, cuando se cierra desde Borrador.</param>
public sealed record CambiarEstadoRequest(EstadoLicitacion Estado, string? Motivo = null);

/// <summary>Licitación tal como se devuelve al exterior.</summary>
/// <param name="Id">Identificador generado por el sistema.</param>
/// <param name="Codigo">Código tal como se registró.</param>
/// <param name="Titulo">Título descriptivo.</param>
/// <param name="Estado">Etapa actual del ciclo de vida.</param>
/// <param name="PresupuestoEstimadoCRC">Presupuesto estimado en colones.</param>
/// <param name="FechaCierre">Instante de cierre, en tiempo universal.</param>
/// <param name="CerradaFuncionalmente">
/// Indica si ya no admite ofertas, sea por su estado o porque la fecha se alcanzó. Es el
/// dato que debe mirarse para decidir si se admiten operaciones, no el estado por sí solo.
/// </param>
/// <param name="CantidadOfertas">Ofertas recibidas.</param>
/// <param name="CreatedAt">Instante de registro.</param>
/// <param name="UpdatedAt">Instante de la última modificación.</param>
public sealed record LicitacionResponse(
    Guid Id,
    string Codigo,
    string Titulo,
    EstadoLicitacion Estado,
    decimal PresupuestoEstimadoCRC,
    DateTimeOffset FechaCierre,
    bool CerradaFuncionalmente,
    int CantidadOfertas,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>Mejor oferta de una licitación, con su ahorro y su clasificación.</summary>
/// <param name="LicitacionId">Licitación consultada.</param>
/// <param name="PresupuestoEstimadoCRC">Presupuesto contra el que se compara.</param>
/// <param name="Oferta">Oferta ganadora, o <c>null</c> si no hay ofertas válidas.</param>
/// <param name="PorcentajeAhorro">Ahorro respecto del presupuesto, o <c>null</c> si no hay ofertas.</param>
/// <param name="Clasificacion">
/// Etiqueta acordada con el cliente: "Sin ofertas válidas", "Oferta conveniente",
/// "Oferta aceptable" u "Oferta válida sin ahorro".
/// </param>
public sealed record MejorOfertaResponse(
    Guid LicitacionId,
    decimal PresupuestoEstimadoCRC,
    OfertaResponse? Oferta,
    decimal? PorcentajeAhorro,
    string Clasificacion);

/// <summary>Filtros del listado de licitaciones.</summary>
public sealed record ConsultaLicitaciones : ConsultaPaginada
{
    /// <summary>Crea la consulta con los valores por omisión.</summary>
    public ConsultaLicitaciones()
    {
    }

    /// <summary>Estado por el que se filtra, si se indica.</summary>
    public EstadoLicitacion? Estado { get; init; }
}
