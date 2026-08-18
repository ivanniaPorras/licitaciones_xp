using Licitaciones.Application.Persistencia;
using Licitaciones.Domain.Proveedores;
using Microsoft.EntityFrameworkCore;

namespace Licitaciones.Infrastructure.Persistencia.Repositorios;

/// <inheritdoc cref="IProveedorRepository" />
public sealed class ProveedorRepository : IProveedorRepository
{
    private readonly LicitacionesDbContext _contexto;

    /// <summary>Crea el repositorio sobre el contexto indicado.</summary>
    /// <param name="contexto">Contexto de acceso a datos.</param>
    public ProveedorRepository(LicitacionesDbContext contexto) => _contexto = contexto;

    /// <inheritdoc />
    public Task<Proveedor?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion = default) =>
        _contexto.Proveedores.SingleOrDefaultAsync(p => p.Id == id, cancelacion);

    /// <inheritdoc />
    public Task<bool> ExisteNombreAsync(
        string nombre,
        Guid? excluyendoId = null,
        CancellationToken cancelacion = default)
    {
        var normalizado = NormalizadorNombreProveedor.Normalizar(nombre);

        return _contexto.Proveedores.AnyAsync(
            p => p.NombreNormalizado == normalizado && (excluyendoId == null || p.Id != excluyendoId),
            cancelacion);
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<Proveedor> Elementos, int Total)> ListarAsync(
        string? busqueda,
        string? orden,
        int pagina,
        int tamano,
        CancellationToken cancelacion = default)
    {
        var consulta = _contexto.Proveedores.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            // Se busca sobre la columna normalizada para que el término encuentre al
            // proveedor sin importar cómo se escribiera su nombre.
            var termino = NormalizadorNombreProveedor.Normalizar(busqueda);
            consulta = consulta.Where(p => p.NombreNormalizado.Contains(termino));
        }

        consulta = orden switch
        {
            "nombre:desc" => consulta.OrderByDescending(p => p.NombreNormalizado),
            "creacion:desc" => consulta.OrderByDescending(p => p.CreatedAt),
            "creacion:asc" => consulta.OrderBy(p => p.CreatedAt),
            _ => consulta.OrderBy(p => p.NombreNormalizado)
        };

        var total = await consulta.CountAsync(cancelacion);
        var elementos = await consulta
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .ToListAsync(cancelacion);

        return (elementos, total);
    }

    /// <inheritdoc />
    public void Agregar(Proveedor proveedor) => _contexto.Proveedores.Add(proveedor);

    /// <inheritdoc />
    public void Eliminar(Proveedor proveedor) => _contexto.Proveedores.Remove(proveedor);
}
