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
    public async Task<(IReadOnlyList<TipoCambio> Elementos, int Total)> ListarAsync(
        int? anioVigencia,
        string? orden,
        int pagina,
        int tamano,
        CancellationToken cancelacion = default)
    {
        var consulta = _contexto.TiposCambio.AsNoTracking();

        // Se filtra por año y no por texto libre porque el listado solo tiene números y
        // fechas: buscar el año es la única búsqueda que alguien escribiría de verdad.
        if (anioVigencia is not null)
        {
            consulta = consulta.Where(t => t.FechaVigencia.Year == anioVigencia);
        }

        consulta = orden switch
        {
            "vigencia:asc" => consulta.OrderBy(t => t.FechaVigencia),
            "tasa:asc" => consulta.OrderBy(t => t.CRCporUSD),
            "tasa:desc" => consulta.OrderByDescending(t => t.CRCporUSD),
            _ => consulta.OrderByDescending(t => t.FechaVigencia)
        };

        var total = await consulta.CountAsync(cancelacion);
        var elementos = await consulta
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .ToListAsync(cancelacion);

        return (elementos, total);
    }

    /// <inheritdoc />
    public void Agregar(TipoCambio tipoCambio) => _contexto.TiposCambio.Add(tipoCambio);

    /// <inheritdoc />
    public void Eliminar(TipoCambio tipoCambio) => _contexto.TiposCambio.Remove(tipoCambio);
}
