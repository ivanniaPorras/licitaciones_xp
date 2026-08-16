using Licitaciones.Application.Persistencia;
using Licitaciones.Domain.Dinero;
using Microsoft.EntityFrameworkCore;

namespace Licitaciones.Infrastructure.Persistencia.Repositorios;

/// <inheritdoc cref="ITipoCambioRepository" />
public sealed class TipoCambioRepository : ITipoCambioRepository
{
    private readonly LicitacionesDbContext _contexto;

    /// <summary>Crea el repositorio sobre el contexto indicado.</summary>
    /// <param name="contexto">Contexto de acceso a datos.</param>
    public TipoCambioRepository(LicitacionesDbContext contexto) => _contexto = contexto;

    /// <inheritdoc />
    public Task<TipoCambio?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion = default) =>
        _contexto.TiposCambio.SingleOrDefaultAsync(t => t.Id == id, cancelacion);

    /// <inheritdoc />
    public Task<TipoCambio?> ObtenerActivoAsync(CancellationToken cancelacion = default) =>
        _contexto.TiposCambio.SingleOrDefaultAsync(t => t.Activo, cancelacion);

    /// <inheritdoc />
    public async Task<IReadOnlyList<TipoCambio>> ObtenerTodosAsync(CancellationToken cancelacion = default) =>
        await _contexto.TiposCambio
            .OrderByDescending(t => t.FechaVigencia)
            .ToListAsync(cancelacion);

    /// <inheritdoc />
    public void Agregar(TipoCambio tipoCambio) => _contexto.TiposCambio.Add(tipoCambio);

    /// <inheritdoc />
    public void Eliminar(TipoCambio tipoCambio) => _contexto.TiposCambio.Remove(tipoCambio);
}
