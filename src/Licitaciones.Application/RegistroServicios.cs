using Licitaciones.Application.Aprobacion;
using Licitaciones.Application.Licitaciones;
using Licitaciones.Application.Ofertas;
using Licitaciones.Application.Proveedores;
using Microsoft.Extensions.DependencyInjection;

namespace Licitaciones.Application;

/// <summary>
/// Registra los servicios de aplicación en el contenedor de dependencias.
/// </summary>
public static class RegistroServicios
{
    /// <summary>Registra los casos de uso de todos los módulos.</summary>
    /// <param name="servicios">Colección de servicios de la aplicación.</param>
    public static IServiceCollection AgregarAplicacion(this IServiceCollection servicios)
    {
        servicios.AddScoped<IProveedorService, ProveedorService>();
        servicios.AddScoped<ILicitacionService, LicitacionService>();
        servicios.AddScoped<IOfertaService, OfertaService>();
        servicios.AddScoped<INivelAprobacionService, NivelAprobacionService>();

        return servicios;
    }
}
