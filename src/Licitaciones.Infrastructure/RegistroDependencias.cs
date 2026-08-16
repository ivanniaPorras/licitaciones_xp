using Licitaciones.Application.Persistencia;
using Licitaciones.Domain.Tiempo;
using Licitaciones.Infrastructure.Persistencia;
using Licitaciones.Infrastructure.Persistencia.Repositorios;
using Licitaciones.Infrastructure.Tiempo;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Licitaciones.Infrastructure;

/// <summary>
/// Registra en el contenedor de dependencias todo lo que aporta la capa de
/// infraestructura. Es el único punto donde las aplicaciones anfitrionas necesitan
/// conocerla.
/// </summary>
public static class RegistroDependencias
{
    /// <summary>Registra el contexto, los repositorios y el reloj del sistema.</summary>
    /// <param name="servicios">Colección de servicios de la aplicación.</param>
    /// <param name="cadenaConexion">Cadena de conexión a PostgreSQL.</param>
    public static IServiceCollection AgregarInfraestructura(
        this IServiceCollection servicios,
        string cadenaConexion)
    {
        servicios.AddSingleton<IClock, SystemClock>();

        servicios.AddDbContext<LicitacionesDbContext>(opciones =>
            opciones.UseNpgsql(cadenaConexion, npgsql => npgsql.EnableRetryOnFailure()));

        servicios.AddScoped<IUnitOfWork, UnitOfWork>();
        servicios.AddScoped<ILicitacionRepository, LicitacionRepository>();
        servicios.AddScoped<IProveedorRepository, ProveedorRepository>();
        servicios.AddScoped<IOfertaRepository, OfertaRepository>();
        servicios.AddScoped<INivelAprobacionRepository, NivelAprobacionRepository>();
        servicios.AddScoped<ITipoCambioRepository, TipoCambioRepository>();

        return servicios;
    }
}
