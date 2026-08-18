using Licitaciones.Infrastructure.Persistencia;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Licitaciones.IntegrationTests.Apoyo;

/// <summary>
/// Levanta la API completa apuntando al contenedor de PostgreSQL de la fixture, de modo
/// que los endpoints se prueben contra infraestructura real y no contra dobles.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _cadenaConexion;

    /// <summary>Crea la fábrica sobre la base indicada.</summary>
    /// <param name="cadenaConexion">Cadena de conexión al contenedor en ejecución.</param>
    public ApiFactory(string cadenaConexion) => _cadenaConexion = cadenaConexion;

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", _cadenaConexion);

        builder.ConfigureTestServices(servicios =>
        {
            // La aplicación registra su propio contexto con la cadena de configuración;
            // aquí se sustituye por la del contenedor para no depender del entorno.
            var descriptor = servicios.SingleOrDefault(
                s => s.ServiceType == typeof(DbContextOptions<LicitacionesDbContext>));
            if (descriptor is not null)
            {
                servicios.Remove(descriptor);
            }

            servicios.AddDbContext<LicitacionesDbContext>(opciones =>
                opciones.UseNpgsql(_cadenaConexion));
        });
    }
}
