using Licitaciones.Application.Persistencia;
using Licitaciones.Domain.Aprobacion;
using Licitaciones.Domain.Dinero;
using Microsoft.EntityFrameworkCore;

namespace Licitaciones.Infrastructure.Persistencia.Repositorios;

/// <inheritdoc cref="INivelAprobacionRepository" />
public sealed class NivelAprobacionRepository : INivelAprobacionRepository
{
    private readonly LicitacionesDbContext _contexto;

    /// <summary>Crea el repositorio sobre el contexto indicado.</summary>
    /// <param name="contexto">Contexto de acceso a datos.</param>
    public NivelAprobacionRepository(LicitacionesDbContext contexto) => _contexto = contexto;

    /// <inheritdoc />
    public Task<NivelAprobacion?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion = default) =>
        _contexto.NivelesAprobacion.SingleOrDefaultAsync(n => n.Id == id, cancelacion);

    /// <inheritdoc />
    public async Task<IReadOnlyList<NivelAprobacion>> ObtenerTodosAsync(
        CancellationToken cancelacion = default) =>
        await _contexto.NivelesAprobacion
            .OrderBy(n => n.MontoMinimo)
            .ToListAsync(cancelacion);

    /// <inheritdoc />
    public Task<NivelAprobacion?> ObtenerAplicableAsync(
        MontoCRC monto,
        CancellationToken cancelacion = default) =>
        // La consulta va contra la tabla: cambiar la política de aprobación es editar
        // filas, no recompilar el programa.
        _contexto.NivelesAprobacion
            .Where(n => monto >= n.MontoMinimo && (n.MontoMaximo == null || monto <= n.MontoMaximo))
            .OrderBy(n => n.MontoMinimo)
            .FirstOrDefaultAsync(cancelacion);

    /// <inheritdoc />
    public async Task<(IReadOnlyList<NivelAprobacion> Elementos, int Total)> ListarAsync(
        string? busqueda,
        string? orden,
        int pagina,
        int tamano,
        CancellationToken cancelacion = default)
    {
        var consulta = _contexto.NivelesAprobacion.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var termino = busqueda.Trim();
            consulta = consulta.Where(n => EF.Functions.ILike(n.Aprobador, $"%{termino}%"));
        }

        consulta = orden switch
        {
            "montoMinimo:desc" => consulta.OrderByDescending(n => n.MontoMinimo),
            "aprobador:asc" => consulta.OrderBy(n => n.Aprobador),
            "aprobador:desc" => consulta.OrderByDescending(n => n.Aprobador),
            _ => consulta.OrderBy(n => n.MontoMinimo)
        };

        var total = await consulta.CountAsync(cancelacion);
        var elementos = await consulta
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .ToListAsync(cancelacion);

        return (elementos, total);
    }

    /// <inheritdoc />
    public void Agregar(NivelAprobacion nivel) => _contexto.NivelesAprobacion.Add(nivel);

    /// <inheritdoc />
    public void Eliminar(NivelAprobacion nivel) => _contexto.NivelesAprobacion.Remove(nivel);
}
