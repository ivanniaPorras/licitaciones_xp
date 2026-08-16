namespace Licitaciones.Application.Persistencia;

/// <summary>
/// Confirma los cambios pendientes y permite agrupar varias operaciones en una sola
/// transacción.
/// </summary>
/// <remarks>
/// Los casos de uso que tocan más de un registro relacionado —activar un tipo de cambio,
/// revalidar los rangos de aprobación— necesitan que todo quede o no quede. Esta
/// abstracción evita que la capa de aplicación tenga que conocer el contexto de Entity
/// Framework Core para conseguirlo.
/// </remarks>
public interface IUnitOfWork
{
    /// <summary>Guarda los cambios pendientes.</summary>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    /// <returns>Cantidad de registros afectados.</returns>
    Task<int> GuardarCambiosAsync(CancellationToken cancelacion = default);

    /// <summary>
    /// Ejecuta la operación dentro de una transacción y la confirma si termina bien. Si
    /// la operación falla, no queda ningún cambio aplicado.
    /// </summary>
    /// <typeparam name="T">Tipo del resultado de la operación.</typeparam>
    /// <param name="operacion">Trabajo que debe ejecutarse de forma atómica.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    Task<T> EjecutarEnTransaccionAsync<T>(
        Func<CancellationToken, Task<T>> operacion,
        CancellationToken cancelacion = default);
}
