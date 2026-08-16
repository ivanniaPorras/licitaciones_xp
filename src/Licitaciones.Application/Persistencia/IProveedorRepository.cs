using Licitaciones.Domain.Proveedores;

namespace Licitaciones.Application.Persistencia;

/// <summary>Acceso a los proveedores almacenados.</summary>
public interface IProveedorRepository
{
    /// <summary>Busca un proveedor por su identificador.</summary>
    /// <param name="id">Identificador del proveedor.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    Task<Proveedor?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion = default);

    /// <summary>
    /// Indica si ya existe un proveedor vigente con ese nombre, comparando por su forma
    /// normalizada.
    /// </summary>
    /// <param name="nombre">Nombre tal como lo escribió la persona usuaria.</param>
    /// <param name="excluyendoId">Proveedor que se está editando, para no compararlo consigo mismo.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    Task<bool> ExisteNombreAsync(
        string nombre,
        Guid? excluyendoId = null,
        CancellationToken cancelacion = default);

    /// <summary>Agrega un proveedor nuevo.</summary>
    /// <param name="proveedor">Proveedor que se va a guardar.</param>
    void Agregar(Proveedor proveedor);

    /// <summary>Da de baja el proveedor. El borrado es lógico.</summary>
    /// <param name="proveedor">Proveedor que se da de baja.</param>
    void Eliminar(Proveedor proveedor);
}
