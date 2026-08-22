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

    public Task<(IReadOnlyList<TipoCambio> Elementos, int Total)> ListarAsync(
        int? anioVigencia,
        string? orden,
        int pagina,
        int tamano,
        CancellationToken cancelacion = default)
    {
        var consulta = _tiposCambio.AsEnumerable();

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

        var filtrados = consulta.ToList();
        var elementos = filtrados.Skip((pagina - 1) * tamano).Take(tamano).ToList();

        return Task.FromResult<(IReadOnlyList<TipoCambio>, int)>((elementos, filtrados.Count));
    }

    public void Agregar(TipoCambio tipoCambio) => _tiposCambio.Add(tipoCambio);

    public void Eliminar(TipoCambio tipoCambio) => _tiposCambio.Remove(tipoCambio);
}
