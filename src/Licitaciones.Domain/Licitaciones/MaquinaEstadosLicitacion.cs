using System.Collections.Frozen;
using System.Collections.Immutable;

namespace Licitaciones.Domain.Licitaciones;

/// <summary>
/// Único punto del sistema que decide qué transiciones de estado admite una licitación.
/// Concentrarlas aquí evita que la regla se disperse en condicionales por los
/// controladores y los servicios.
/// </summary>
public static class MaquinaEstadosLicitacion
{
    private static readonly FrozenDictionary<EstadoLicitacion, ImmutableArray<EstadoLicitacion>> TransicionesPermitidas =
        new Dictionary<EstadoLicitacion, ImmutableArray<EstadoLicitacion>>
        {
            [EstadoLicitacion.Borrador] = [EstadoLicitacion.Publicada, EstadoLicitacion.Cerrada],
            [EstadoLicitacion.Publicada] = [EstadoLicitacion.Cerrada],
            [EstadoLicitacion.Cerrada] = []
        }.ToFrozenDictionary();

    /// <summary>Estados a los que puede pasar una licitación desde el estado indicado.</summary>
    /// <param name="origen">Estado actual de la licitación.</param>
    public static ImmutableArray<EstadoLicitacion> TransicionesDesde(EstadoLicitacion origen) =>
        TransicionesPermitidas.TryGetValue(origen, out var destinos) ? destinos : [];

    /// <summary>Indica si la transición entre los dos estados está permitida.</summary>
    /// <param name="origen">Estado actual de la licitación.</param>
    /// <param name="destino">Estado al que se quiere cambiar.</param>
    public static bool EsTransicionPermitida(EstadoLicitacion origen, EstadoLicitacion destino) =>
        TransicionesDesde(origen).Contains(destino);

    /// <summary>Comprueba la transición y la rechaza si el ciclo de vida no la admite.</summary>
    /// <param name="origen">Estado actual de la licitación.</param>
    /// <param name="destino">Estado al que se quiere cambiar.</param>
    /// <exception cref="TransicionEstadoInvalidaException">Si la transición no está permitida.</exception>
    public static void Validar(EstadoLicitacion origen, EstadoLicitacion destino)
    {
        if (!EsTransicionPermitida(origen, destino))
        {
            throw new TransicionEstadoInvalidaException(origen, destino);
        }
    }
}
