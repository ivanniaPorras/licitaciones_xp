using Licitaciones.Domain.Dinero;
using Licitaciones.Domain.Tiempo;

namespace Licitaciones.Domain.Licitaciones;

/// <summary>
/// Proceso de compra publicado por la organización. Es la raíz que gobierna su propio
/// ciclo de estados y el período durante el cual admite ofertas.
/// </summary>
public sealed class Licitacion
{
    private Licitacion(string codigo, string titulo, MontoCRC presupuestoEstimado, DateTimeOffset fechaCierre)
    {
        Codigo = codigo;
        CodigoNormalizado = NormalizadorCodigo.Normalizar(codigo);
        Titulo = titulo;
        PresupuestoEstimado = presupuestoEstimado;
        FechaCierre = fechaCierre;
        Estado = EstadoLicitacion.Borrador;
    }

    /// <summary>Identificador generado por el sistema. No es editable por la persona usuaria.</summary>
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>Código tal como lo escribió la persona usuaria.</summary>
    public string Codigo { get; private set; }

    /// <summary>Forma normalizada del código, que es la que se compara para detectar duplicados.</summary>
    public string CodigoNormalizado { get; private set; }

    /// <summary>Título descriptivo del proceso de compra.</summary>
    public string Titulo { get; private set; }

    /// <summary>Etapa actual del ciclo de vida.</summary>
    public EstadoLicitacion Estado { get; private set; }

    /// <summary>Monto máximo autorizado, en colones.</summary>
    public MontoCRC PresupuestoEstimado { get; private set; }

    /// <summary>Instante a partir del cual la licitación deja de admitir ofertas.</summary>
    public DateTimeOffset FechaCierre { get; private set; }

    /// <summary>Crea una licitación en estado Borrador.</summary>
    /// <param name="codigo">Código identificador del proceso.</param>
    /// <param name="titulo">Título descriptivo.</param>
    /// <param name="presupuestoEstimadoCRC">Presupuesto estimado en colones.</param>
    /// <param name="fechaCierre">Instante de cierre de la recepción de ofertas.</param>
    public static Licitacion Crear(
        string codigo,
        string titulo,
        decimal presupuestoEstimadoCRC,
        DateTimeOffset fechaCierre) =>
        new(codigo, titulo, MontoCRC.Crear(presupuestoEstimadoCRC), fechaCierre);

    /// <summary>
    /// Indica si la licitación ya no admite ofertas. Devuelve verdadero tanto si su estado
    /// es Cerrada como si la fecha de cierre ya se alcanzó, aunque el estado almacenado
    /// siga siendo Publicada porque nadie lo actualizó. Toda validación sobre ofertas debe
    /// consultar este método y no el campo de estado por separado.
    /// </summary>
    /// <param name="reloj">Reloj del que se obtiene el instante actual.</param>
    public bool EstaCerradaFuncionalmente(IClock reloj) =>
        Estado == EstadoLicitacion.Cerrada || reloj.UtcNow >= FechaCierre.ToUniversalTime();

    /// <summary>Cambia el estado si el ciclo de vida admite la transición.</summary>
    /// <param name="destino">Estado al que se quiere pasar.</param>
    /// <exception cref="TransicionEstadoInvalidaException">Si la transición no está permitida.</exception>
    public void CambiarEstado(EstadoLicitacion destino)
    {
        MaquinaEstadosLicitacion.Validar(Estado, destino);
        Estado = destino;
    }
}
