using Licitaciones.Application.Persistencia;
using Licitaciones.Domain.Licitaciones;

namespace Licitaciones.UnitTests.Apoyo.Dobles;

/// <summary>Repositorio de licitaciones en memoria para las pruebas del servicio.</summary>
public sealed class RepositorioLicitacionesEnMemoria : ILicitacionRepository
{
    private readonly List<Licitacion> _licitaciones = [];

    public IReadOnlyList<Licitacion> Contenido => _licitaciones;

    public void Sembrar(params Licitacion[] licitaciones) => _licitaciones.AddRange(licitaciones);

    public Task<Licitacion?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion = default) =>
        Task.FromResult(_licitaciones.SingleOrDefault(l => l.Id == id));

    public Task<bool> ExisteCodigoAsync(
        string codigo,
        Guid? excluyendoId = null,
        CancellationToken cancelacion = default)
    {
        var normalizado = NormalizadorCodigo.Normalizar(codigo);

        return Task.FromResult(_licitaciones.Any(l =>
            l.CodigoNormalizado == normalizado && (excluyendoId is null || l.Id != excluyendoId)));
    }

    public Task<(IReadOnlyList<Licitacion> Elementos, int Total)> ListarAsync(
        string? busqueda,
        string? orden,
        EstadoLicitacion? estado,
        int pagina,
        int tamano,
        CancellationToken cancelacion = default)
    {
        IEnumerable<Licitacion> consulta = _licitaciones;

        if (estado is { } filtro)
        {
            consulta = consulta.Where(l => l.Estado == filtro);
        }

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var termino = NormalizadorCodigo.Normalizar(busqueda);
            consulta = consulta.Where(l =>
                l.CodigoNormalizado.Contains(termino, StringComparison.Ordinal)
                || l.Titulo.Contains(busqueda, StringComparison.OrdinalIgnoreCase));
        }

        consulta = orden switch
        {
            "codigo:desc" => consulta.OrderByDescending(l => l.CodigoNormalizado, StringComparer.Ordinal),
            "codigo:asc" => consulta.OrderBy(l => l.CodigoNormalizado, StringComparer.Ordinal),
            "fechaCierre:asc" => consulta.OrderBy(l => l.FechaCierre),
            _ => consulta.OrderByDescending(l => l.FechaCierre)
        };

        var materializada = consulta.ToList();
        var pagina1 = materializada.Skip((pagina - 1) * tamano).Take(tamano).ToList();

        return Task.FromResult<(IReadOnlyList<Licitacion>, int)>((pagina1, materializada.Count));
    }

    public void Agregar(Licitacion licitacion) => _licitaciones.Add(licitacion);

    public void Eliminar(Licitacion licitacion) => _licitaciones.Remove(licitacion);
}
