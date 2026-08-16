namespace Licitaciones.Domain.Tiempo;

/// <summary>
/// Abstracción del reloj del sistema. El dominio y la capa de aplicación nunca consultan
/// la hora directamente: la reciben por aquí, de modo que las pruebas puedan fijar un
/// instante concreto y los casos frontera de vencimiento sean deterministas.
/// </summary>
public interface IClock
{
    /// <summary>Instante actual expresado en tiempo universal coordinado.</summary>
    DateTimeOffset UtcNow { get; }
}
