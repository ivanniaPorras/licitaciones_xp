using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Licitaciones.Infrastructure.Persistencia.Configuraciones;

/// <summary>
/// Configuración de la concurrencia optimista sobre PostgreSQL.
/// </summary>
internal static class ConcurrenciaExtensions
{
    /// <summary>Nombre de la columna de sistema que PostgreSQL mantiene en cada tabla.</summary>
    public const string ColumnaXmin = "xmin";

    /// <summary>
    /// Usa la columna de sistema <c>xmin</c> como testigo de concurrencia optimista.
    /// </summary>
    /// <remarks>
    /// PostgreSQL guarda en <c>xmin</c> el identificador de la transacción que escribió
    /// cada fila por última vez, así que cambia sola en cada actualización y no hace falta
    /// añadir ni mantener una columna de versión propia. Se declara como propiedad sombra
    /// para que el dominio no tenga que conocerla.
    /// </remarks>
    /// <typeparam name="T">Entidad que se configura.</typeparam>
    /// <param name="constructor">Constructor de la entidad.</param>
    public static EntityTypeBuilder<T> UsarXminComoTestigoDeConcurrencia<T>(
        this EntityTypeBuilder<T> constructor)
        where T : class
    {
        constructor.Property<uint>(ColumnaXmin)
            .HasColumnName(ColumnaXmin)
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        return constructor;
    }
}
