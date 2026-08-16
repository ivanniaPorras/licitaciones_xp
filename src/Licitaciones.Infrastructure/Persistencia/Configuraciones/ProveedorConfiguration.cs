using Licitaciones.Domain.Proveedores;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Licitaciones.Infrastructure.Persistencia.Configuraciones;

/// <summary>Mapeo de la entidad <see cref="Proveedor"/>.</summary>
public sealed class ProveedorConfiguration : IEntityTypeConfiguration<Proveedor>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Proveedor> constructor)
    {
        constructor.ToTable("proveedores");

        constructor.HasKey(p => p.Id);
        constructor.Property(p => p.Id).HasColumnName("id").ValueGeneratedNever();

        constructor.Property(p => p.Nombre)
            .HasColumnName("nombre").HasMaxLength(200).IsRequired();

        constructor.Property(p => p.NombreNormalizado)
            .HasColumnName("nombre_normalizado").HasMaxLength(200).IsRequired();

        constructor.Property(p => p.CreatedAt)
            .HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();

        constructor.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();

        constructor.Property(p => p.DeletedAt)
            .HasColumnName("deleted_at").HasColumnType("timestamp with time zone");

        constructor.HasIndex(p => p.NombreNormalizado)
            .HasDatabaseName("ix_proveedores_nombre_normalizado")
            .IsUnique()
            .HasFilter("deleted_at IS NULL");

        constructor.UsarXminComoTestigoDeConcurrencia();
        constructor.HasQueryFilter(p => p.DeletedAt == null);
    }
}
