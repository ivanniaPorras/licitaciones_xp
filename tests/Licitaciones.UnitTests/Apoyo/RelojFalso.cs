using Licitaciones.Domain.Tiempo;

namespace Licitaciones.UnitTests.Apoyo;

/// <summary>
/// Reloj con una hora fija, para que las pruebas de vencimiento sean deterministas y no
/// dependan de la hora real de la máquina que las ejecuta.
/// </summary>
public sealed class RelojFalso : IClock
{
    private DateTimeOffset _ahora;

    public RelojFalso(DateTimeOffset ahora) => _ahora = ahora;

    public DateTimeOffset UtcNow => _ahora.ToUniversalTime();

    /// <summary>Mueve el reloj al instante indicado.</summary>
    public void Situar(DateTimeOffset ahora) => _ahora = ahora;

    /// <summary>Adelanta el reloj el tiempo indicado.</summary>
    public void Avanzar(TimeSpan cuanto) => _ahora = _ahora.Add(cuanto);
}
