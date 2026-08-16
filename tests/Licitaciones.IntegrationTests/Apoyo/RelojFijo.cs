using Licitaciones.Domain.Tiempo;

namespace Licitaciones.IntegrationTests.Apoyo;

/// <summary>
/// Reloj con una hora que la prueba controla, para poder afirmar sobre las fechas de
/// auditoría que asigna la infraestructura.
/// </summary>
public sealed class RelojFijo : IClock
{
    private DateTimeOffset _ahora;

    public RelojFijo(DateTimeOffset ahora) => _ahora = ahora;

    public DateTimeOffset UtcNow => _ahora.ToUniversalTime();

    /// <summary>Mueve el reloj al instante indicado.</summary>
    public void Situar(DateTimeOffset ahora) => _ahora = ahora;

    /// <summary>Adelanta el reloj el tiempo indicado.</summary>
    public void Avanzar(TimeSpan cuanto) => _ahora = _ahora.Add(cuanto);
}
