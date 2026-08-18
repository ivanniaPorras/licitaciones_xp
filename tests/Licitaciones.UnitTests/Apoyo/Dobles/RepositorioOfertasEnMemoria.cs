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

    public void Agregar(Oferta oferta) => _ofertas.Add(oferta);

    public void Eliminar(Oferta oferta) => _ofertas.Remove(oferta);
}
