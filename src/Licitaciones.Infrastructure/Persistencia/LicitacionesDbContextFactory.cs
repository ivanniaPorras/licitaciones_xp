using Licitaciones.Infrastructure.Tiempo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Licitaciones.Infrastructure.Persistencia;

/// <summary>
/// Construye el contexto cuando las herramientas de línea de comandos generan o aplican
/// migraciones. Solo se usa en tiempo de diseño: la aplicación en ejecución obtiene el
/// contexto por inyección de dependencias.
/// </summary>
/// <remarks>
/// La cadena de conexión sale de la variable de entorno <c>ConnectionStrings__Default</c>.
/// El valor de reserva apunta a la base local de desarrollo y no contiene credenciales
/// reales; generar una migración no se conecta a la base, solo necesita conocer el
/// proveedor.
/// </remarks>
public sealed class LicitacionesDbContextFactory : IDesignTimeDbContextFactory<LicitacionesDbContext>
{
    /// <inheritdoc />
    public LicitacionesDbContext CreateDbContext(string[] args)
    {
        var cadenaConexion =
            Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "Host=localhost;Port=5432;Database=licitaciones;Username=postgres";

        var opciones = new DbContextOptionsBuilder<LicitacionesDbContext>()
            .UseNpgsql(cadenaConexion)
            .Options;

        return new LicitacionesDbContext(opciones, new SystemClock());
    }
}
