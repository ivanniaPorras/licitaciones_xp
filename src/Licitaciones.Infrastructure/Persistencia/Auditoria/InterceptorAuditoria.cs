using Licitaciones.Domain.Auditoria;
using Licitaciones.Domain.Tiempo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Licitaciones.Infrastructure.Persistencia.Auditoria;

/// <summary>
/// Rellena las fechas de auditoría y convierte las eliminaciones en borrados lógicos justo
/// antes de guardar.
/// </summary>
/// <remarks>
/// Escribe a través del seguimiento de cambios y no de las propiedades de la entidad,
/// porque el dominio expone esas fechas solo para lectura: quién y cuándo las asigna es
/// una decisión de infraestructura.
/// </remarks>
public static class InterceptorAuditoria
{
    /// <summary>Nombre de la propiedad que guarda el instante de creación.</summary>
    public const string CreatedAt = nameof(IAuditable.CreatedAt);

    /// <summary>Nombre de la propiedad que guarda el instante de la última modificación.</summary>
    public const string UpdatedAt = nameof(IAuditable.UpdatedAt);

    /// <summary>Nombre de la propiedad que guarda el instante del borrado lógico.</summary>
    public const string DeletedAt = nameof(ISoftDeletable.DeletedAt);

    /// <summary>Aplica la auditoría a todas las entidades pendientes de guardar.</summary>
    /// <param name="seguimiento">Seguimiento de cambios del contexto.</param>
    /// <param name="reloj">Reloj del que se toma el instante actual.</param>
    public static void Aplicar(ChangeTracker seguimiento, IClock reloj)
    {
        var ahora = reloj.UtcNow;

        foreach (var entrada in seguimiento.Entries<IAuditable>())
        {
            switch (entrada.State)
            {
                case EntityState.Added:
                    entrada.Property(CreatedAt).CurrentValue = ahora;
                    entrada.Property(UpdatedAt).CurrentValue = ahora;
                    break;

                case EntityState.Modified:
                    entrada.Property(UpdatedAt).CurrentValue = ahora;
                    entrada.Property(CreatedAt).IsModified = false;
                    break;
            }
        }

        foreach (var entrada in seguimiento.Entries<ISoftDeletable>())
        {
            if (entrada.State != EntityState.Deleted)
            {
                continue;
            }

            // La entidad admite borrado lógico: en lugar de suprimir la fila se marca la
            // fecha de baja, para no dejar huérfanas las ofertas que la referencian.
            entrada.State = EntityState.Modified;
            entrada.Property(DeletedAt).CurrentValue = ahora;
            entrada.Property(UpdatedAt).CurrentValue = ahora;
        }
    }
}
