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
    public void Agregar(Licitacion licitacion) => _contexto.Licitaciones.Add(licitacion);

    /// <inheritdoc />
    public void Eliminar(Licitacion licitacion) => _contexto.Licitaciones.Remove(licitacion);
}
