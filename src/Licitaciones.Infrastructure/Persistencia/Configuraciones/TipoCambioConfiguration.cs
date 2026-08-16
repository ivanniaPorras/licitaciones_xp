using Licitaciones.Domain.Dinero;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Licitaciones.Infrastructure.Persistencia.Configuraciones;

/// <summary>Mapeo de la entidad <see cref="TipoCambio"/>.</summary>
public sealed class TipoCambioConfiguration : IEntityTypeConfiguration<TipoCambio>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TipoCambio> constructor)
    {
        constructor.ToTable("tipos_cambio", tabla => tabla.HasCheckConstraint(
            "ck_tipos_cambio_tasa_positiva",
            "crc_por_usd > 0"));

        constructor.HasKey(t => t.Id);
        constructor.Property(t => t.Id).HasColumnName("id").ValueGeneratedNever();

        // La tasa admite cuatro decimales porque es un factor de conversión y no una
        // cantidad de dinero que deba cuadrar al céntimo.
        constructor.Property(t => t.CRCporUSD)
            .HasColumnName("crc_por_usd").HasColumnType("numeric(18,4)").IsRequired();

        constructor.Property(t => t.FechaVigencia)
            .HasColumnName("fecha_vigencia").HasColumnType("timestamp with time zone").IsRequired();

        constructor.Property(t => t.Activo).HasColumnName("activo").IsRequired();

        constructor.Property(t => t.CreatedAt)
            .HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();

        constructor.Property(t => t.UpdatedAt)
            .HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();

        // Índice único parcial: la base garantiza por sí sola que no haya dos tasas
        // activas, aunque una condición de carrera burle la comprobación en el servidor.
        constructor.HasIndex(t => t.Activo)
            .HasDatabaseName("ix_tipos_cambio_unico_activo")
            .IsUnique()
            .HasFilter("activo = true");

        constructor.UsarXminComoTestigoDeConcurrencia();
    }
}
