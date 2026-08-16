using Licitaciones.Domain.Aprobacion;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Licitaciones.Infrastructure.Persistencia.Configuraciones;

/// <summary>Mapeo de la entidad <see cref="NivelAprobacion"/>.</summary>
public sealed class NivelAprobacionConfiguration : IEntityTypeConfiguration<NivelAprobacion>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<NivelAprobacion> constructor)
    {
        constructor.ToTable("niveles_aprobacion", tabla => tabla.HasCheckConstraint(
            "ck_niveles_rango_valido",
            "monto_minimo_crc > 0 AND (monto_maximo_crc IS NULL OR monto_maximo_crc >= monto_minimo_crc)"));

        constructor.HasKey(n => n.Id);
        constructor.Property(n => n.Id).HasColumnName("id").ValueGeneratedNever();

        constructor.Property(n => n.MontoMinimo)
            .HasColumnName("monto_minimo_crc")
            .HasConversion(ConversoresDinero.Monto)
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        constructor.Property(n => n.MontoMaximo)
            .HasColumnName("monto_maximo_crc")
            .HasConversion(ConversoresDinero.MontoOpcional)
            .HasColumnType("numeric(18,2)");

        constructor.Property(n => n.Aprobador)
            .HasColumnName("aprobador").HasMaxLength(100).IsRequired();

        constructor.Property(n => n.CreatedAt)
            .HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();

        constructor.Property(n => n.UpdatedAt)
            .HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();

        constructor.Ignore(n => n.EsRangoAbierto);

        constructor.HasIndex(n => n.MontoMinimo)
            .HasDatabaseName("ix_niveles_aprobacion_monto_minimo");

        constructor.UsarXminComoTestigoDeConcurrencia();
    }
}
