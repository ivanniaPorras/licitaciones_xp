using Licitaciones.Application.Persistencia;
using Licitaciones.Domain.Dinero;

namespace Licitaciones.UnitTests.Apoyo.Dobles;

/// <summary>Repositorio de tipos de cambio en memoria para las pruebas del servicio.</summary>
public sealed class RepositorioTiposCambioEnMemoria : ITipoCambioRepository
{
    private readonly List<TipoCambio> _tiposCambio = [];

    public IReadOnlyList<TipoCambio> Contenido => _tiposCambio;

    public void Sembrar(params TipoCambio[] tiposCambio) => _tiposCambio.AddRange(tiposCambio);

    public Task<TipoCambio?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion = default) =>
        Task.FromResult(_tiposCambio.SingleOrDefault(t => t.Id == id));

    public Task<TipoCambio?> ObtenerActivoAsync(CancellationToken cancelacion = default) =>
        Task.FromResult(_tiposCambio.SingleOrDefault(t => t.Activo));

    public Task<IReadOnlyList<TipoCambio>> ObtenerTodosAsync(CancellationToken cancelacion = default) =>
        Task.FromResult<IReadOnlyList<TipoCambio>>([.. _tiposCambio.OrderByDescending(t => t.FechaVigencia)]);

    public void Agregar(TipoCambio tipoCambio) => _tiposCambio.Add(tipoCambio);

    public void Eliminar(TipoCambio tipoCambio) => _tiposCambio.Remove(tipoCambio);
}
