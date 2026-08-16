namespace Licitaciones.Infrastructure.Persistencia.Errores;

/// <summary>
/// Códigos de estado SQL que PostgreSQL devuelve en los fallos que este sistema traduce a
/// mensajes controlados.
/// </summary>
public static class EstadosSqlPostgres
{
    /// <summary>Violación de un índice o restricción de unicidad.</summary>
    public const string ViolacionUnicidad = "23505";

    /// <summary>Violación de una clave foránea.</summary>
    public const string ViolacionIntegridadReferencial = "23503";

    /// <summary>Violación de una restricción de verificación.</summary>
    public const string ViolacionRestriccionVerificacion = "23514";
}
