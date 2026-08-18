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

    public void Agregar(NivelAprobacion nivel) => _niveles.Add(nivel);

    public void Eliminar(NivelAprobacion nivel) => _niveles.Remove(nivel);
}
