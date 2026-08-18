using Licitaciones.Application.Persistencia;
using Licitaciones.Domain.Proveedores;

namespace Licitaciones.UnitTests.Apoyo.Dobles;

/// <summary>
/// Repositorio de proveedores en memoria. Permite probar las reglas del servicio de
/// aplicación sin base de datos; la unicidad contra PostgreSQL se verifica aparte, en las
/// pruebas de integración.
/// </summary>
public sealed class RepositorioProveedoresEnMemoria : IProveedorRepository
{
    private readonly List<Proveedor> _proveedores = [];

    public IReadOnlyList<Proveedor> Contenido => _proveedores;

    public void Sembrar(params Proveedor[] proveedores) => _proveedores.AddRange(proveedores);

    public Task<Proveedor?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion = default) =>
        Task.FromResult(_proveedores.SingleOrDefault(p => p.Id == id));

    public Task<bool> ExisteNombreAsync(
        string nombre,
        Guid? excluyendoId = null,
        CancellationToken cancelacion = default)
    {
        var normalizado = NormalizadorNombreProveedor.Normalizar(nombre);

        return Task.FromResult(_proveedores.Any(p =>
            p.NombreNormalizado == normalizado && (excluyendoId is null || p.Id != excluyendoId)));
    }

    public Task<(IReadOnlyList<Proveedor> Elementos, int Total)> ListarAsync(
        string? busqueda,
        string? orden,
        int pagina,
        int tamano,
        CancellationToken cancelacion = default)
    {
        IEnumerable<Proveedor> consulta = _proveedores;

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var termino = NormalizadorNombreProveedor.Normalizar(busqueda);
            consulta = consulta.Where(p => p.NombreNormalizado.Contains(termino, StringComparison.Ordinal));
        }

        consulta = orden switch
        {
            "nombre:desc" => consulta.OrderByDescending(p => p.NombreNormalizado, StringComparer.Ordinal),
            "creacion:desc" => consulta.OrderByDescending(p => p.CreatedAt),
            "creacion:asc" => consulta.OrderBy(p => p.CreatedAt),
            _ => consulta.OrderBy(p => p.NombreNormalizado, StringComparer.Ordinal)
        };

        var materializada = consulta.ToList();
        var pagina1 = materializada.Skip((pagina - 1) * tamano).Take(tamano).ToList();

        return Task.FromResult<(IReadOnlyList<Proveedor>, int)>((pagina1, materializada.Count));
    }

    public void Agregar(Proveedor proveedor) => _proveedores.Add(proveedor);

    public void Eliminar(Proveedor proveedor) => _proveedores.Remove(proveedor);
}
