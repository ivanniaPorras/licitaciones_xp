using Licitaciones.Domain.Licitaciones;

namespace Licitaciones.Application.Persistencia;

/// <summary>Acceso a las licitaciones almacenadas.</summary>
public interface ILicitacionRepository
{
    /// <summary>Busca una licitación por su identificador.</summary>
    /// <param name="id">Identificador de la licitación.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    Task<Licitacion?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion = default);

    /// <summary>
    /// Indica si ya existe una licitación vigente con ese código, comparando por su forma
    /// normalizada.
    /// </summary>
    /// <param name="codigo">Código tal como lo escribió la persona usuaria.</param>
    /// <param name="excluyendoId">Licitación que se está editando, para no compararla consigo misma.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    Task<bool> ExisteCodigoAsync(
        string codigo,
        Guid? excluyendoId = null,
        CancellationToken cancelacion = default);

    /// <summary>Agrega una licitación nueva.</summary>
    /// <param name="licitacion">Licitación que se va a guardar.</param>
    void Agregar(Licitacion licitacion);

    /// <summary>Da de baja la licitación. El borrado es lógico.</summary>
    /// <param name="licitacion">Licitación que se da de baja.</param>
    void Eliminar(Licitacion licitacion);
}
