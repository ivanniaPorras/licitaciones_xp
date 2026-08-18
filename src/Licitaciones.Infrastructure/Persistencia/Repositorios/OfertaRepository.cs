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

        var proyeccion = Proyectar();

        if (consulta.LicitacionId is { } licitacionId)
        {
            proyeccion = proyeccion.Where(o => o.LicitacionId == licitacionId);
        }

        if (consulta.ProveedorId is { } proveedorId)
        {
            proyeccion = proyeccion.Where(o => o.ProveedorId == proveedorId);
        }

        if (!string.IsNullOrWhiteSpace(consulta.Busqueda))
        {
            var termino = consulta.Busqueda.Trim();
            proyeccion = proyeccion.Where(o =>
                o.CodigoLicitacion.Contains(termino) || o.NombreProveedor.Contains(termino));
        }

        proyeccion = consulta.Orden switch
        {
            "monto:desc" => proyeccion.OrderByDescending(o => o.MontoOfertadoCRC),
            "monto:asc" => proyeccion.OrderBy(o => o.MontoOfertadoCRC),
            "fecha:asc" => proyeccion.OrderBy(o => o.FechaRegistro),
            _ => proyeccion.OrderByDescending(o => o.FechaRegistro)
        };

        var total = await proyeccion.CountAsync(cancelacion);
        var elementos = await proyeccion
            .Skip((consulta.Pagina - 1) * consulta.Tamano)
            .Take(consulta.Tamano)
            .ToListAsync(cancelacion);

        return (elementos, total);
    }

    /// <inheritdoc />
    public Task<OfertaResponse?> ObtenerDetalleAsync(Guid id, CancellationToken cancelacion = default) =>
        Proyectar().SingleOrDefaultAsync(o => o.Id == id, cancelacion);

    // La combinación con licitación y proveedor se resuelve en la propia consulta. Se
    // ignoran los filtros globales para que una oferta siga siendo legible aunque su
    // licitación o su proveedor se hayan dado de baja: la oferta es evidencia del proceso.
    private IQueryable<OfertaResponse> Proyectar() =>
        from oferta in _contexto.Ofertas.AsNoTracking()
        join licitacion in _contexto.Licitaciones.AsNoTracking().IgnoreQueryFilters()
            on oferta.LicitacionId equals licitacion.Id
        join proveedor in _contexto.Proveedores.AsNoTracking().IgnoreQueryFilters()
            on oferta.ProveedorId equals proveedor.Id
        select new OfertaResponse(
            oferta.Id,
            oferta.LicitacionId,
            licitacion.Codigo,
            oferta.ProveedorId,
            proveedor.Nombre,
            oferta.Monto.Valor,
            oferta.FechaRegistro);

    /// <inheritdoc />
    public void Agregar(Oferta oferta) => _contexto.Ofertas.Add(oferta);

    /// <inheritdoc />
    public void Eliminar(Oferta oferta) => _contexto.Ofertas.Remove(oferta);
}
