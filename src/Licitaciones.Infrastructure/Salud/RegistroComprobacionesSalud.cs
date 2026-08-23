using Licitaciones.Infrastructure.Persistencia;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Licitaciones.Infrastructure.Salud;

/// <summary>
/// Registra las comprobaciones de salud que usan Docker Compose y Kubernetes para saber si
/// un contenedor está vivo y si ya puede recibir tráfico.
/// </summary>
/// <remarks>
/// Se distinguen dos preguntas distintas. <b>Vitalidad</b> es "el proceso responde"; si
/// falla, hay que reiniciar el contenedor. <b>Disponibilidad</b> es "el proceso responde y
/// además alcanza la base"; si falla, el contenedor sigue vivo pero no debe recibir
/// peticiones todavía. Reiniciar por no alcanzar la base solo produciría un ciclo de
/// reinicios mientras la base tarda en levantar.
/// </remarks>
public static class RegistroComprobacionesSalud
{
    /// <summary>Etiqueta de las comprobaciones que consultan la base de datos.</summary>
    public const string EtiquetaBaseDatos = "base-datos";

    /// <summary>Agrega la comprobación de la base de datos.</summary>
    /// <param name="servicios">Colección de servicios de la aplicación.</param>
    public static IServiceCollection AgregarComprobacionesSalud(this IServiceCollection servicios)
    {
        servicios.AddHealthChecks()
            .AddDbContextCheck<LicitacionesDbContext>(
                name: "postgres",
                failureStatus: HealthStatus.Unhealthy,
                tags: [EtiquetaBaseDatos]);

        return servicios;
    }
}
