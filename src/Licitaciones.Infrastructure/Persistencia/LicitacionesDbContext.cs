using Licitaciones.Domain.Aprobacion;
using Licitaciones.Domain.Dinero;
using Licitaciones.Domain.Licitaciones;
using Licitaciones.Domain.Ofertas;
using Licitaciones.Domain.Proveedores;
using Licitaciones.Domain.Tiempo;
using Licitaciones.Infrastructure.Persistencia.Auditoria;
using Licitaciones.Infrastructure.Persistencia.Configuraciones;
using Microsoft.EntityFrameworkCore;

namespace Licitaciones.Infrastructure.Persistencia;

/// <summary>
/// Contexto de acceso a datos del sistema. Configura el mapeo de cada entidad mediante
/// clases separadas y aplica la auditoría automática al guardar.
/// </summary>
public sealed class LicitacionesDbContext : DbContext
{
    private readonly IClock _reloj;

    /// <summary>Crea el contexto.</summary>
    /// <param name="opciones">Opciones de configuración del contexto.</param>
    /// <param name="reloj">Reloj del que se toman las fechas de auditoría.</param>
    public LicitacionesDbContext(DbContextOptions<LicitacionesDbContext> opciones, IClock reloj)
        : base(opciones) => _reloj = reloj;

    /// <summary>Licitaciones vigentes. Las dadas de baja quedan excluidas por el filtro global.</summary>
    public DbSet<Licitacion> Licitaciones => Set<Licitacion>();

    /// <summary>Proveedores vigentes. Los dados de baja quedan excluidos por el filtro global.</summary>
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();

    /// <summary>Ofertas presentadas.</summary>
    public DbSet<Oferta> Ofertas => Set<Oferta>();

    /// <summary>Rangos de aprobación parametrizables.</summary>
    public DbSet<NivelAprobacion> NivelesAprobacion => Set<NivelAprobacion>();

    /// <summary>Tipos de cambio registrados.</summary>
    public DbSet<TipoCambio> TiposCambio => Set<TipoCambio>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelo)
    {
        modelo.ApplyConfigurationsFromAssembly(typeof(LicitacionesDbContext).Assembly);
        SembradorDatosIniciales.Sembrar(modelo);
    }

    /// <inheritdoc />
    protected override void ConfigureConventions(ModelConfigurationBuilder configuracion)
    {
        // Se aplica a todas las fechas del modelo en lugar de repetirlo columna por
        // columna: ninguna fecha debe llegar a la base con un desplazamiento distinto
        // de cero.
        configuracion.Properties<DateTimeOffset>().HaveConversion<ConversorFechaAUtc>();
        configuracion.Properties<DateTimeOffset?>().HaveConversion<ConversorFechaOpcionalAUtc>();
    }

    /// <inheritdoc />
    public override int SaveChanges()
    {
        InterceptorAuditoria.Aplicar(ChangeTracker, _reloj);
        return base.SaveChanges();
    }

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        InterceptorAuditoria.Aplicar(ChangeTracker, _reloj);
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
}
