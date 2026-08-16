namespace Licitaciones.Domain.Licitaciones;

/// <summary>
/// Etapa del ciclo de vida de una licitación.
/// </summary>
public enum EstadoLicitacion
{
    /// <summary>En preparación. Todavía no recibe ofertas.</summary>
    Borrador = 1,

    /// <summary>Publicada y recibiendo ofertas hasta su fecha de cierre.</summary>
    Publicada = 2,

    /// <summary>Terminada. Estado final: no admite ninguna transición posterior.</summary>
    Cerrada = 3
}
