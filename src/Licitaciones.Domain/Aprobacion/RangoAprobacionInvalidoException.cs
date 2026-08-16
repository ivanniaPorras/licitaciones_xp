using Licitaciones.Domain.Excepciones;

namespace Licitaciones.Domain.Aprobacion;

/// <summary>
/// Se produce cuando un nivel de aprobación se define con un rango incoherente o sin
/// persona aprobadora.
/// </summary>
public sealed class RangoAprobacionInvalidoException : ReglaNegocioException
{
    /// <summary>Crea la excepción con el mensaje que verá la persona usuaria.</summary>
    /// <param name="mensaje">Texto controlado que describe el incumplimiento.</param>
    public RangoAprobacionInvalidoException(string mensaje) : base(mensaje)
    {
    }
}
