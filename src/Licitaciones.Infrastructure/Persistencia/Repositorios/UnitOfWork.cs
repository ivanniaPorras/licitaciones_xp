using Licitaciones.Application.Persistencia;
using Licitaciones.Infrastructure.Persistencia.Errores;
using Microsoft.EntityFrameworkCore;

namespace Licitaciones.Infrastructure.Persistencia.Repositorios;

/// <summary>Confirma los cambios del contexto y agrupa operaciones en transacciones.</summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly LicitacionesDbContext _contexto;

    /// <summary>Crea la unidad de trabajo sobre el contexto indicado.</summary>
    /// <param name="contexto">Contexto de acceso a datos.</param>
    public UnitOfWork(LicitacionesDbContext contexto) => _contexto = contexto;

    /// <inheritdoc />
    public async Task<int> GuardarCambiosAsync(CancellationToken cancelacion = default)
    {
        try
        {
            return await _contexto.SaveChangesAsync(cancelacion);
        }
        catch (DbUpdateException error) when (TraductorErroresPostgres.Traducir(error) is { } controlado)
        {
            // Se traduce aquí para que ninguna capa superior tenga que conocer los códigos
            // de estado ni los nombres de índice de PostgreSQL.
            throw controlado;
        }
    }

    /// <inheritdoc />
    public async Task<T> EjecutarEnTransaccionAsync<T>(
        Func<CancellationToken, Task<T>> operacion,
        CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(operacion);

        // Si ya hay una transacción en curso, la operación se suma a ella en lugar de
        // abrir una anidada, que PostgreSQL no admite.
        if (_contexto.Database.CurrentTransaction is not null)
        {
            return await operacion(cancelacion);
        }

        var estrategia = _contexto.Database.CreateExecutionStrategy();

        return await estrategia.ExecuteAsync(async () =>
        {
            await using var transaccion = await _contexto.Database.BeginTransactionAsync(cancelacion);

            var resultado = await operacion(cancelacion);
            await GuardarCambiosAsync(cancelacion);
            await transaccion.CommitAsync(cancelacion);

            return resultado;
        });
    }
}
