using Licitaciones.Application.Persistencia;
using Licitaciones.Domain.Aprobacion;
using Licitaciones.Domain.Dinero;

namespace Licitaciones.UnitTests.Apoyo.Dobles;

/// <summary>Repositorio de niveles de aprobación en memoria para las pruebas del servicio.</summary>
public sealed class RepositorioNivelesAprobacionEnMemoria : INivelAprobacionRepository
{
    private readonly List<NivelAprobacion> _niveles = [];

    public IReadOnlyList<NivelAprobacion> Contenido => _niveles;

    public void Sembrar(params NivelAprobacion[] niveles) => _niveles.AddRange(niveles);

    public Task<NivelAprobacion?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion = default) =>
        Task.FromResult(_niveles.SingleOrDefault(n => n.Id == id));

    public Task<IReadOnlyList<NivelAprobacion>> ObtenerTodosAsync(CancellationToken cancelacion = default) =>
        Task.FromResult<IReadOnlyList<NivelAprobacion>>([.. _niveles.OrderBy(n => n.MontoMinimo)]);

    public Task<NivelAprobacion?> ObtenerAplicableAsync(
        MontoCRC monto,
        CancellationToken cancelacion = default) =>
        Task.FromResult(_niveles.OrderBy(n => n.MontoMinimo).FirstOrDefault(n => n.Cubre(monto)));

    public Task<(IReadOnlyList<NivelAprobacion> Elementos, int Total)> ListarAsync(
        string? busqueda,
        string? orden,
        int pagina,
        int tamano,
        CancellationToken cancelacion = default)
    {
        var consulta = _niveles.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            consulta = consulta.Where(n => n.Aprobador.Contains(
                busqueda.Trim(),
                StringComparison.OrdinalIgnoreCase));
        }

        consulta = orden switch
        {
            "montoMinimo:desc" => consulta.OrderByDescending(n => n.MontoMinimo),
            "aprobador:asc" => consulta.OrderBy(n => n.Aprobador, StringComparer.Ordinal),
            "aprobador:desc" => consulta.OrderByDescending(n => n.Aprobador, StringComparer.Ordinal),
            _ => consulta.OrderBy(n => n.MontoMinimo)
        };

        var filtrados = consulta.ToList();
        var elementos = filtrados.Skip((pagina - 1) * tamano).Take(tamano).ToList();

        return Task.FromResult<(IReadOnlyList<NivelAprobacion>, int)>((elementos, filtrados.Count));
    }

    public void Agregar(NivelAprobacion nivel) => _niveles.Add(nivel);

    public void Eliminar(NivelAprobacion nivel) => _niveles.Remove(nivel);
}
