using Licitaciones.Domain.Excepciones;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Licitaciones.Infrastructure.Persistencia.Errores;

/// <summary>
/// Convierte los fallos que devuelve PostgreSQL en errores de negocio con un mensaje
/// comprensible.
/// </summary>
/// <remarks>
/// Existe porque el mensaje crudo del motor no debe llegar nunca a la persona usuaria:
/// revela nombres de tablas, de índices y detalles de la instalación. La traducción se
/// apoya en el nombre de la restricción que falló, que es información que este proyecto
/// controla porque define esos nombres explícitamente en las configuraciones.
/// </remarks>
public static class TraductorErroresPostgres
{
    private static readonly Dictionary<string, string> MensajesPorRestriccion = new(StringComparer.Ordinal)
    {
        ["ix_licitaciones_codigo_normalizado"] = "Ya existe una licitación con ese código.",
        ["ix_proveedores_nombre_normalizado"] = "Ya existe un proveedor con ese nombre.",
        ["ix_ofertas_licitacion_proveedor"] = "Este proveedor ya registró una oferta para esta licitación.",
        ["ix_tipos_cambio_unico_activo"] = "Ya hay un tipo de cambio activo.",
        ["ck_licitaciones_presupuesto_positivo"] = "El presupuesto debe ser mayor que cero.",
        ["ck_ofertas_monto_positivo"] = "El monto ofertado debe ser mayor que cero.",
        ["ck_tipos_cambio_tasa_positiva"] = "El tipo de cambio debe ser mayor que cero.",
        ["ck_niveles_rango_valido"] = "El rango del nivel de aprobación no es válido."
    };

    /// <summary>
    /// Devuelve el error de negocio equivalente, o <c>null</c> si el fallo no corresponde a
    /// una restricción conocida y debe tratarse como error no controlado.
    /// </summary>
    /// <param name="error">Fallo devuelto al guardar los cambios.</param>
    public static ReglaNegocioException? Traducir(DbUpdateException error)
    {
        if (error.InnerException is not PostgresException fallo)
        {
            return null;
        }

        if (fallo.ConstraintName is { } restriccion
            && MensajesPorRestriccion.TryGetValue(restriccion, out var mensaje))
        {
            return new ReglaNegocioException(mensaje);
        }

        return fallo.SqlState switch
        {
            EstadosSqlPostgres.ViolacionIntegridadReferencial =>
                new ReglaNegocioException("No se puede completar la operación: existen registros relacionados."),
            EstadosSqlPostgres.ViolacionUnicidad =>
                new ReglaNegocioException("Ya existe un registro con esos datos."),
            EstadosSqlPostgres.ViolacionRestriccionVerificacion =>
                new ReglaNegocioException("Los datos no cumplen una regla del sistema."),
            _ => null
        };
    }
}
