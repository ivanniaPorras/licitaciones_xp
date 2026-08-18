using Licitaciones.Application.Persistencia;
using Licitaciones.Domain.Licitaciones;
using Microsoft.EntityFrameworkCore;

namespace Licitaciones.Infrastructure.Persistencia.Repositorios;

/// <inheritdoc cref="ILicitacionRepository" />
public sealed class LicitacionRepository : ILicitacionRepository
{
    private readonly LicitacionesDbContext _contexto;

    /// <summary>Crea el repositorio sobre el contexto indicado.</summary>
    /// <param name="contexto">Contexto de acceso a datos.</param>
    public LicitacionRepository(LicitacionesDbContext contexto) => _contexto = contexto;

    /// <inheritdoc />
    public Task<Licitacion?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion = default) =>
        _contexto.Licitaciones.SingleOrDefaultAsync(l => l.Id == id, cancelacion);

    /// <inheritdoc />
    public Task<bool> ExisteCodigoAsync(
        string codigo,
        Guid? excluyendoId = null,
        CancellationToken cancelacion = default)
    {
        // Se compara la forma normalizada, que es la misma que respalda el índice único.
        var normalizado = NormalizadorCodigo.Normalizar(codigo);

        return _contexto.Licitaciones.AnyAsync(
            l => l.CodigoNormalizado == normalizado && (excluyendoId == null || l.Id != excluyendoId),
            cancelacion);
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<Licitacion> Elementos, int Total)> ListarAsync(
        string? busqueda,
        string? orden,
        EstadoLicitacion? estado,
        int pagina,
        int tamano,
        CancellationToken cancelacion = default)
    {
        var consulta = _contexto.Licitaciones.AsNoTracking();

        if (estado is { } filtro)
        {
            consulta = consulta.Where(l => l.Estado == filtro);
        }

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var normalizado = NormalizadorCodigo.Normalizar(busqueda);
            var termino = busqueda.Trim();
            consulta = consulta.Where(l =>
                l.CodigoNormalizado.Contains(normalizado) || l.Titulo.Contains(termino));
        }

        consulta = orden switch
        {
            "codigo:desc" => consulta.OrderByDescending(l => l.CodigoNormalizado),
            "codigo:asc" => consulta.OrderBy(l => l.CodigoNormalizado),
            "fechaCierre:asc" => consulta.OrderBy(l => l.FechaCierre),
            _ => consulta.OrderByDescending(l => l.FechaCierre)
        };

        var total = await consulta.CountAsync(cancelacion);
        var elementos = await consulta
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .ToListAsync(cancelacion);

        return (elementos, total);
    }

    /// <inheritdoc />
    public void Agregar(Licitacion licitacion) => _contexto.Licitaciones.Add(licitacion);

    /// <inheritdoc />
    public void Eliminar(Licitacion licitacion) => _contexto.Licitaciones.Remove(licitacion);
}
