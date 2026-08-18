using Licitaciones.Application.Persistencia;

namespace Licitaciones.UnitTests.Apoyo.Dobles;

/// <summary>
/// Unidad de trabajo que solo cuenta las confirmaciones. Los repositorios en memoria ya
/// aplican los cambios al agregarlos, así que aquí no hay nada que persistir.
/// </summary>
public sealed class UnidadDeTrabajoFalsa : IUnitOfWork
{
    public int Confirmaciones { get; private set; }

    public Task<int> GuardarCambiosAsync(CancellationToken cancelacion = default)
    {
        Confirmaciones++;
        return Task.FromResult(1);
    }

    public Task<T> EjecutarEnTransaccionAsync<T>(
        Func<CancellationToken, Task<T>> operacion,
        CancellationToken cancelacion = default) =>
        operacion(cancelacion);
}
