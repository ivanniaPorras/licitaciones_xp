using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Licitaciones.Infrastructure.Persistencia;

/// <summary>
/// Aplica las migraciones pendientes como un paso aparte del arranque de la aplicación.
/// </summary>
/// <remarks>
/// Migrar al arrancar parece cómodo pero no lo es: con varias réplicas, todas intentarían
/// migrar a la vez sobre la misma base. Por eso las migraciones se ejecutan en un paso
/// propio —un servicio de Docker Compose o un Job de Kubernetes— que corre una sola vez,
/// termina, y solo entonces deja arrancar a la aplicación.
/// </remarks>
public static class MigradorBaseDatos
{
    /// <summary>Argumento de línea de comandos que pide migrar y salir.</summary>
    public const string Argumento = "--aplicar-migraciones";

    /// <summary>Indica si los argumentos piden ejecutar únicamente las migraciones.</summary>
    /// <param name="argumentos">Argumentos con los que se invocó el programa.</param>
    public static bool SePidioMigrar(string[] argumentos) =>
        argumentos is not null && argumentos.Contains(Argumento, StringComparer.Ordinal);

    /// <summary>Aplica las migraciones pendientes y devuelve el código de salida.</summary>
    /// <param name="servicios">Proveedor con el contexto ya registrado.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    /// <returns>Cero si el esquema quedó al día, uno si algo falló.</returns>
    public static async Task<int> AplicarAsync(
        IServiceProvider servicios,
        CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(servicios);

        await using var alcance = servicios.CreateAsyncScope();
        var contexto = alcance.ServiceProvider.GetRequiredService<LicitacionesDbContext>();
        var registro = alcance.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(nameof(MigradorBaseDatos));

        try
        {
            var pendientes = (await contexto.Database.GetPendingMigrationsAsync(cancelacion)).ToList();
            if (pendientes.Count == 0)
            {
                registro.LogInformation("El esquema ya estaba al día. No había migraciones pendientes.");
                return 0;
            }

            registro.LogInformation(
                "Aplicando {Cantidad} migraciones pendientes: {Migraciones}",
                pendientes.Count,
                string.Join(", ", pendientes));

            await contexto.Database.MigrateAsync(cancelacion);

            registro.LogInformation("Migraciones aplicadas correctamente.");
            return 0;
        }
        catch (Exception error)
        {
            // Se devuelve un código de salida distinto de cero para que el orquestador vea
            // el fallo y no deje arrancar la aplicación sobre un esquema incompleto.
            registro.LogError(error, "No se pudieron aplicar las migraciones.");
            return 1;
        }
    }
}
