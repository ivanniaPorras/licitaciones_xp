using Licitaciones.Application.Ofertas;
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
    public async Task<(IReadOnlyList<OfertaResponse> Elementos, int Total)> ListarDetalleAsync(
        ConsultaOfertas consulta,
        CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(consulta);

        // Los filtros y el orden se aplican sobre las entidades, no sobre el resultado
        // proyectado: PostgreSQL solo entiende las columnas de las tablas, y una vez
        // proyectado el objeto la consulta ya no se puede traducir.
        var consultaOfertas = _contexto.Ofertas.AsNoTracking();

        if (consulta.LicitacionId is { } licitacionId)
        {
            consultaOfertas = consultaOfertas.Where(o => o.LicitacionId == licitacionId);
        }

        if (consulta.ProveedorId is { } proveedorId)
        {
            consultaOfertas = consultaOfertas.Where(o => o.ProveedorId == proveedorId);
        }

        consultaOfertas = consulta.Orden switch
        {
            "monto:desc" => consultaOfertas.OrderByDescending(o => o.Monto),
            "monto:asc" => consultaOfertas.OrderBy(o => o.Monto),
            "fecha:asc" => consultaOfertas.OrderBy(o => o.FechaRegistro),
            _ => consultaOfertas.OrderByDescending(o => o.FechaRegistro)
        };

        var total = await consultaOfertas.CountAsync(cancelacion);
        var pagina = await consultaOfertas
            .Skip((consulta.Pagina - 1) * consulta.Tamano)
            .Take(consulta.Tamano)
            .ToListAsync(cancelacion);

        var elementos = await CombinarConDetalleAsync(pagina, cancelacion);

        // La búsqueda textual actúa sobre el código de la licitación y el nombre del
        // proveedor, que viven en otras tablas; se aplica tras combinar.
        if (!string.IsNullOrWhiteSpace(consulta.Busqueda))
        {
            var termino = consulta.Busqueda.Trim();
            elementos =
            [
                .. elementos.Where(o =>
                    o.CodigoLicitacion.Contains(termino, StringComparison.OrdinalIgnoreCase)
                    || o.NombreProveedor.Contains(termino, StringComparison.OrdinalIgnoreCase))
            ];
        }

        return (elementos, total);
    }

    /// <inheritdoc />
    public async Task<OfertaResponse?> ObtenerDetalleAsync(Guid id, CancellationToken cancelacion = default)
    {
        var oferta = await _contexto.Ofertas.AsNoTracking()
            .SingleOrDefaultAsync(o => o.Id == id, cancelacion);

        if (oferta is null)
        {
            return null;
        }

        var combinadas = await CombinarConDetalleAsync([oferta], cancelacion);

        return combinadas.Count == 0 ? null : combinadas[0];
    }

    /// <summary>
    /// Añade a cada oferta el código de su licitación y el nombre de su proveedor.
    /// </summary>
    /// <remarks>
    /// Se ignoran los filtros globales de borrado lógico para que una oferta siga siendo
    /// legible aunque su licitación o su proveedor se hayan dado de baja: la oferta es
    /// evidencia del proceso y no puede quedar sin contexto.
    /// </remarks>
    private async Task<List<OfertaResponse>> CombinarConDetalleAsync(
        List<Oferta> ofertas,
        CancellationToken cancelacion)
    {
        if (ofertas.Count == 0)
        {
            return [];
        }

        var licitacionIds = ofertas.Select(o => o.LicitacionId).Distinct().ToList();
        var proveedorIds = ofertas.Select(o => o.ProveedorId).Distinct().ToList();

        var codigos = await _contexto.Licitaciones.AsNoTracking().IgnoreQueryFilters()
            .Where(l => licitacionIds.Contains(l.Id))
            .Select(l => new { l.Id, l.Codigo })
            .ToDictionaryAsync(l => l.Id, l => l.Codigo, cancelacion);

        var nombres = await _contexto.Proveedores.AsNoTracking().IgnoreQueryFilters()
            .Where(p => proveedorIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Nombre })
            .ToDictionaryAsync(p => p.Id, p => p.Nombre, cancelacion);

        return
        [
            .. ofertas.Select(o => new OfertaResponse(
                o.Id,
                o.LicitacionId,
                codigos.GetValueOrDefault(o.LicitacionId, string.Empty),
                o.ProveedorId,
                nombres.GetValueOrDefault(o.ProveedorId, string.Empty),
                o.Monto.Valor,
                o.FechaRegistro))
        ];
    }

    /// <inheritdoc />
    public void Agregar(Oferta oferta) => _contexto.Ofertas.Add(oferta);

    /// <inheritdoc />
    public void Eliminar(Oferta oferta) => _contexto.Ofertas.Remove(oferta);
}
