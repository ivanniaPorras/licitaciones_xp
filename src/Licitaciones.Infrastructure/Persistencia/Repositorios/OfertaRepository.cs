using Licitaciones.Application.Persistencia;
using Licitaciones.Domain.Dinero;
using Licitaciones.Domain.Ofertas;
using Microsoft.EntityFrameworkCore;

namespace Licitaciones.Infrastructure.Persistencia.Repositorios;

/// <inheritdoc cref="IOfertaRepository" />
public sealed class OfertaRepository : IOfertaRepository
{
    private readonly LicitacionesDbContext _contexto;

    /// <summary>Crea el repositorio sobre el contexto indicado.</summary>
    /// <param name="contexto">Contexto de acceso a datos.</param>
    public OfertaRepository(LicitacionesDbContext contexto) => _contexto = contexto;

    /// <inheritdoc />
    public Task<Oferta?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion = default) =>
        _contexto.Ofertas.SingleOrDefaultAsync(o => o.Id == id, cancelacion);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Oferta>> ObtenerPorLicitacionAsync(
        Guid licitacionId,
        CancellationToken cancelacion = default) =>
        await _contexto.Ofertas
            .Where(o => o.LicitacionId == licitacionId)
            .OrderBy(o => o.Monto)
            .ThenBy(o => o.FechaRegistro)
            .ThenBy(o => o.Id)
            .ToListAsync(cancelacion);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Oferta>> ObtenerPorProveedorAsync(
        Guid proveedorId,
        CancellationToken cancelacion = default) =>
        await _contexto.Ofertas
            .Where(o => o.ProveedorId == proveedorId)
            .OrderByDescending(o => o.FechaRegistro)
            .ToListAsync(cancelacion);

    /// <inheritdoc />
    public Task<bool> ExisteOfertaDelProveedorAsync(
        Guid licitacionId,
        Guid proveedorId,
        CancellationToken cancelacion = default) =>
        _contexto.Ofertas.AnyAsync(
            o => o.LicitacionId == licitacionId && o.ProveedorId == proveedorId,
            cancelacion);

    /// <inheritdoc />
    public async Task<MontoCRC?> ObtenerMontoMaximoAsync(
        Guid licitacionId,
        CancellationToken cancelacion = default)
    {
        var montos = await _contexto.Ofertas
            .Where(o => o.LicitacionId == licitacionId)
            .Select(o => o.Monto)
            .ToListAsync(cancelacion);

        return montos.Count == 0 ? null : montos.Max();
    }

    /// <inheritdoc />
    public Task<bool> TieneOfertasLaLicitacionAsync(
        Guid licitacionId,
        CancellationToken cancelacion = default) =>
        _contexto.Ofertas.AnyAsync(o => o.LicitacionId == licitacionId, cancelacion);

    /// <inheritdoc />
    public Task<bool> TieneOfertasElProveedorAsync(
        Guid proveedorId,
        CancellationToken cancelacion = default) =>
        _contexto.Ofertas.AnyAsync(o => o.ProveedorId == proveedorId, cancelacion);

    /// <inheritdoc />
    public void Agregar(Oferta oferta) => _contexto.Ofertas.Add(oferta);

    /// <inheritdoc />
    public void Eliminar(Oferta oferta) => _contexto.Ofertas.Remove(oferta);
}
