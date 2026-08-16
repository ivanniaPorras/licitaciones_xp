namespace Licitaciones.Domain.Excepciones;

/// <summary>
/// Error provocado por el incumplimiento de una regla de negocio. Su mensaje está
/// redactado para mostrarse a la persona usuaria sin exponer detalles internos.
/// </summary>
public class ReglaNegocioException : Exception
{
    /// <summary>Crea la excepción con el mensaje que verá la persona usuaria.</summary>
    /// <param name="mensaje">Texto controlado que describe la regla incumplida.</param>
    public ReglaNegocioException(string mensaje) : base(mensaje)
    {
    }
}
