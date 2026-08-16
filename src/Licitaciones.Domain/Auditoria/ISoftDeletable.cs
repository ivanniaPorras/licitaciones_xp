namespace Licitaciones.Domain.Auditoria;

/// <summary>
/// Entidad que no se elimina físicamente, sino que se marca con la fecha de su borrado.
/// </summary>
/// <remarks>
/// El borrado lógico es lo que permite conservar las ofertas asociadas a una licitación o
/// a un proveedor dados de baja: si el registro se suprimiera, la evidencia del proceso
/// quedaría huérfana o se perdería.
/// </remarks>
public interface ISoftDeletable
{
    /// <summary>Instante en que se dio de baja el registro, o <c>null</c> si sigue vigente.</summary>
    DateTimeOffset? DeletedAt { get; }
}
