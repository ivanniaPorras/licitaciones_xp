namespace Licitaciones.Application.Comun;

/// <summary>
/// Naturaleza de un error de negocio. Determina el código HTTP con que la API responde.
/// </summary>
public enum TipoError
{
    /// <summary>
    /// Los datos se interpretaron bien pero violan una regla de negocio. Responde 422.
    /// </summary>
    Validacion = 1,

    /// <summary>
    /// El estado actual del sistema impide la operación: duplicados, transiciones no
    /// permitidas, dependencias. Responde 409.
    /// </summary>
    Conflicto = 2,

    /// <summary>El recurso solicitado no existe. Responde 404.</summary>
    NoEncontrado = 3
}
