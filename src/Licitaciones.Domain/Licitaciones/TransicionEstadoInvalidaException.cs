using Licitaciones.Domain.Excepciones;

namespace Licitaciones.Domain.Licitaciones;

/// <summary>
/// Se produce al intentar una transición de estado que el ciclo de vida no admite.
/// </summary>
public sealed class TransicionEstadoInvalidaException : ReglaNegocioException
{
    /// <summary>Crea la excepción indicando el estado de origen y el de destino.</summary>
    /// <param name="origen">Estado actual de la licitación.</param>
    /// <param name="destino">Estado al que se intentó cambiar.</param>
    public TransicionEstadoInvalidaException(EstadoLicitacion origen, EstadoLicitacion destino)
        : base("Transición de estado no permitida.")
    {
        Origen = origen;
        Destino = destino;
    }

    /// <summary>Estado desde el que se intentó la transición.</summary>
    public EstadoLicitacion Origen { get; }

    /// <summary>Estado al que se intentó llegar.</summary>
    public EstadoLicitacion Destino { get; }
}
