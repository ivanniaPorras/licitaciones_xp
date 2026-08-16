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
    public void Agregar(Proveedor proveedor) => _contexto.Proveedores.Add(proveedor);

    /// <inheritdoc />
    public void Eliminar(Proveedor proveedor) => _contexto.Proveedores.Remove(proveedor);
}
