using Licitaciones.Domain.Dinero;
using Licitaciones.Domain.Licitaciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Licitaciones.Infrastructure.Persistencia.Configuraciones;

/// <summary>Mapeo de la entidad <see cref="Licitacion"/>.</summary>
public sealed class LicitacionConfiguration : IEntityTypeConfiguration<Licitacion>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Licitacion> constructor)
    {
        constructor.ToTable("licitaciones", tabla => tabla.HasCheckConstraint(
            "ck_licitaciones_presupuesto_positivo",
            "presupuesto_estimado_crc > 0"));

        constructor.HasKey(l => l.Id);
        constructor.Property(l => l.Id).HasColumnName("id").ValueGeneratedNever();

        constructor.Property(l => l.Codigo)
            .HasColumnName("codigo").HasMaxLength(50).IsRequired();

        constructor.Property(l => l.CodigoNormalizado)
            .HasColumnName("codigo_normalizado").HasMaxLength(50).IsRequired();

        constructor.Property(l => l.Titulo)
            .HasColumnName("titulo").HasMaxLength(200).IsRequired();

        constructor.Property(l => l.Estado)
            .HasColumnName("estado").HasConversion<int>().IsRequired();

        constructor.Property(l => l.PresupuestoEstimado)
            .HasColumnName("presupuesto_estimado_crc")
            .HasConversion(ConversoresDinero.Monto)
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        constructor.Property(l => l.FechaCierre)
            .HasColumnName("fecha_cierre").HasColumnType("timestamp with time zone").IsRequired();

        constructor.Property(l => l.CreatedAt)
            .HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();

        constructor.Property(l => l.UpdatedAt)
            .HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();

        constructor.Property(l => l.DeletedAt)
            .HasColumnName("deleted_at").HasColumnType("timestamp with time zone");

        // El índice es parcial: dos licitaciones pueden compartir código si una de ellas
        // ya fue dada de baja, porque el código dejó de identificar un proceso vigente.
        constructor.HasIndex(l => l.CodigoNormalizado)
            .HasDatabaseName("ix_licitaciones_codigo_normalizado")
            .IsUnique()
            .HasFilter("deleted_at IS NULL");

        constructor.HasIndex(l => new { l.Estado, l.FechaCierre })
            .HasDatabaseName("ix_licitaciones_estado_fecha_cierre");

        constructor.UsarXminComoTestigoDeConcurrencia();
        constructor.HasQueryFilter(l => l.DeletedAt == null);
    }
}
