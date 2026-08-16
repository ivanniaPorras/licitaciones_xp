using Licitaciones.Domain.Licitaciones;
using Licitaciones.Domain.Ofertas;
using Licitaciones.Domain.Proveedores;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Licitaciones.Infrastructure.Persistencia.Configuraciones;

/// <summary>Mapeo de la entidad <see cref="Oferta"/>.</summary>
public sealed class OfertaConfiguration : IEntityTypeConfiguration<Oferta>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Oferta> constructor)
    {
        constructor.ToTable("ofertas", tabla => tabla.HasCheckConstraint(
            "ck_ofertas_monto_positivo",
            "monto_ofertado_crc > 0"));

        constructor.HasKey(o => o.Id);
        constructor.Property(o => o.Id).HasColumnName("id").ValueGeneratedNever();

        constructor.Property(o => o.LicitacionId).HasColumnName("licitacion_id").IsRequired();
        constructor.Property(o => o.ProveedorId).HasColumnName("proveedor_id").IsRequired();

        constructor.Property(o => o.Monto)
            .HasColumnName("monto_ofertado_crc")
            .HasConversion(ConversoresDinero.Monto)
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        constructor.Property(o => o.FechaRegistro)
            .HasColumnName("fecha_registro").HasColumnType("timestamp with time zone").IsRequired();

        constructor.Property(o => o.CreatedAt)
            .HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();

        constructor.Property(o => o.UpdatedAt)
            .HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();

        // Implementa la regla de que un proveedor presenta a lo sumo una oferta por
        // licitación. No es un índice parcial porque la oferta no admite borrado lógico.
        constructor.HasIndex(o => new { o.LicitacionId, o.ProveedorId })
            .HasDatabaseName("ix_ofertas_licitacion_proveedor")
            .IsUnique();

        constructor.HasIndex(o => new { o.LicitacionId, o.Monto, o.FechaRegistro })
            .HasDatabaseName("ix_ofertas_mejor_oferta");

        // Restringir el borrado impide que una licitación o un proveedor con ofertas
        // desaparezcan físicamente y dejen la evidencia huérfana.
        constructor.HasOne<Licitacion>()
            .WithMany()
            .HasForeignKey(o => o.LicitacionId)
            .OnDelete(DeleteBehavior.Restrict);

        constructor.HasOne<Proveedor>()
            .WithMany()
            .HasForeignKey(o => o.ProveedorId)
            .OnDelete(DeleteBehavior.Restrict);

        constructor.UsarXminComoTestigoDeConcurrencia();
    }
}
