using Licitaciones.Domain.Tiempo;

namespace Licitaciones.Infrastructure.Tiempo;

/// <summary>
/// Reloj real del sistema. Es la única clase del proyecto autorizada a consultar la hora
/// directamente; vive en infraestructura para que el dominio no dependa de ella.
/// </summary>
public sealed class SystemClock : IClock
{
    /// <inheritdoc />
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
