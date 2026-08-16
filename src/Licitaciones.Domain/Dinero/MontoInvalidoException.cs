using Licitaciones.Domain.Excepciones;

namespace Licitaciones.Domain.Dinero;

/// <summary>
/// Se produce cuando un monto no cumple las reglas monetarias del sistema: ser mayor que
/// cero y tener a lo sumo dos decimales.
/// </summary>
public sealed class MontoInvalidoException : ReglaNegocioException
{
    /// <summary>Crea la excepción con el mensaje que verá la persona usuaria.</summary>
    /// <param name="mensaje">Texto controlado que describe el incumplimiento.</param>
    public MontoInvalidoException(string mensaje) : base(mensaje)
    {
    }
}
