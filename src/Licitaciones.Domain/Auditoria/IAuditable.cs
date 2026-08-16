namespace Licitaciones.Domain.Auditoria;

/// <summary>
/// Entidad de la que se registra cuándo se creó y cuándo se modificó por última vez.
/// </summary>
/// <remarks>
/// Las fechas se exponen solo para lectura: las asigna la infraestructura al guardar,
/// tomando la hora de <see cref="Tiempo.IClock"/>. El dominio no las fija por su cuenta
/// porque no tiene por qué conocer el reloj para construirse.
/// </remarks>
public interface IAuditable
{
    /// <summary>Instante en que se creó el registro.</summary>
    DateTimeOffset CreatedAt { get; }

    /// <summary>Instante de la última modificación del registro.</summary>
    DateTimeOffset UpdatedAt { get; }
}
