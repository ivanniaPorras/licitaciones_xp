using Licitaciones.Application.Ofertas;
using Licitaciones.Application.Persistencia;
using Licitaciones.Domain.Dinero;
using Licitaciones.Domain.Ofertas;

namespace Licitaciones.UnitTests.Apoyo.Dobles;

/// <summary>Repositorio de ofertas en memoria para las pruebas de los servicios.</summary>
public sealed class RepositorioOfertasEnMemoria : IOfertaRepository
{
    private readonly List<Oferta> _ofertas = [];

    public IReadOnlyList<Oferta> Contenido => _ofertas;

    public void Sembrar(params Oferta[] ofertas) => _ofertas.AddRange(ofertas);

    public Task<Oferta?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion = default) =>
        Task.FromResult(_ofertas.SingleOrDefault(o => o.Id == id));

    public Task<IReadOnlyList<Oferta>> ObtenerPorLicitacionAsync(
        Guid licitacionId,
        CancellationToken cancelacion = default) =>
        Task.FromResult<IReadOnlyList<Oferta>>(
            [.. _ofertas.Where(o => o.LicitacionId == licitacionId)
                        .OrderBy(o => o.Monto.Valor)
                        .ThenBy(o => o.FechaRegistro)
                        .ThenBy(o => o.Id)]);

    public Task<IReadOnlyList<Oferta>> ObtenerPorProveedorAsync(
        Guid proveedorId,
        CancellationToken cancelacion = default) =>
        Task.FromResult<IReadOnlyList<Oferta>>(
            [.. _ofertas.Where(o => o.ProveedorId == proveedorId)
                        .OrderByDescending(o => o.FechaRegistro)]);

    public Task<bool> ExisteOfertaDelProveedorAsync(
        Guid licitacionId,
        Guid proveedorId,
        CancellationToken cancelacion = default) =>
        Task.FromResult(_ofertas.Any(o => o.LicitacionId == licitacionId && o.ProveedorId == proveedorId));

    public Task<MontoCRC?> ObtenerMontoMaximoAsync(
        Guid licitacionId,
        CancellationToken cancelacion = default)
    {
        var montos = _ofertas.Where(o => o.LicitacionId == licitacionId).Select(o => o.Monto).ToList();

        return Task.FromResult<MontoCRC?>(montos.Count == 0 ? null : montos.Max());
    }

    public Task<bool> TieneOfertasLaLicitacionAsync(
        Guid licitacionId,
        CancellationToken cancelacion = default) =>
        Task.FromResult(_ofertas.Any(o => o.LicitacionId == licitacionId));

    public Task<bool> TieneOfertasElProveedorAsync(
        Guid proveedorId,
        CancellationToken cancelacion = default) =>
        Task.FromResult(_ofertas.Any(o => o.ProveedorId == proveedorId));

    public Task<(IReadOnlyList<OfertaResponse> Elementos, int Total)> ListarDetalleAsync(
        ConsultaOfertas consulta,
        CancellationToken cancelacion = default)
    {
        IEnumerable<Oferta> consultadas = _ofertas;

        if (consulta.LicitacionId is { } licitacionId)
        {
            consultadas = consultadas.Where(o => o.LicitacionId == licitacionId);
        }

        if (consulta.ProveedorId is { } proveedorId)
        {
            consultadas = consultadas.Where(o => o.ProveedorId == proveedorId);
        }

        var materializadas = consultadas.OrderByDescending(o => o.FechaRegistro).ToList();
        var pagina = materializadas
            .Skip((consulta.Pagina - 1) * consulta.Tamano)
            .Take(consulta.Tamano)
            .Select(ADetalle)
            .ToList();

        return Task.FromResult<(IReadOnlyList<OfertaResponse>, int)>((pagina, materializadas.Count));
    }

    public Task<OfertaResponse?> ObtenerDetalleAsync(Guid id, CancellationToken cancelacion = default) =>
        Task.FromResult(_ofertas.SingleOrDefault(o => o.Id == id) is { } oferta ? ADetalle(oferta) : null);

    // El doble no conoce licitaciones ni proveedores: los nombres se dejan vacíos porque
    // ninguna regla de negocio depende de ellos, solo la presentación.
    private static OfertaResponse ADetalle(Oferta oferta) => new(
        oferta.Id,
        oferta.LicitacionId,
        string.Empty,
        oferta.ProveedorId,
        string.Empty,
        oferta.Monto.Valor,
        oferta.FechaRegistro);

    public void Agregar(Oferta oferta) => _ofertas.Add(oferta);

    public void Eliminar(Oferta oferta) => _ofertas.Remove(oferta);
}
