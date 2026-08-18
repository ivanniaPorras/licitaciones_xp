using System.Globalization;
using Licitaciones.Application.Aprobacion;
using Licitaciones.Application.Comun;
using Licitaciones.Application.Ofertas;
using Licitaciones.Application.Persistencia;
using Licitaciones.Domain.Dinero;
using Licitaciones.Domain.Licitaciones;
using Licitaciones.Domain.Ofertas;
using Licitaciones.Domain.Tiempo;

namespace Licitaciones.Application.Licitaciones;

/// <inheritdoc cref="ILicitacionService" />
public sealed class LicitacionService : ILicitacionService
{
    private readonly ILicitacionRepository _licitaciones;
    private readonly IOfertaRepository _ofertas;
    private readonly IUnitOfWork _unidadDeTrabajo;
    private readonly IClock _reloj;
    private readonly INivelAprobacionService _niveles;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    /// <param name="licitaciones">Acceso a las licitaciones.</param>
    /// <param name="ofertas">Acceso a las ofertas.</param>
    /// <param name="unidadDeTrabajo">Confirmación de los cambios.</param>
    /// <param name="reloj">Reloj del que se toma el instante actual.</param>
    /// <param name="niveles">Casos de uso de niveles, para resolver el aprobador.</param>
    public LicitacionService(
        ILicitacionRepository licitaciones,
        IOfertaRepository ofertas,
        IUnitOfWork unidadDeTrabajo,
        IClock reloj,
        INivelAprobacionService niveles)
    {
        _licitaciones = licitaciones;
        _ofertas = ofertas;
        _unidadDeTrabajo = unidadDeTrabajo;
        _reloj = reloj;
        _niveles = niveles;
    }

    /// <inheritdoc />
    public async Task<Result<LicitacionResponse>> CrearAsync(
        CrearLicitacionRequest peticion,
        CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(peticion);

        if (string.IsNullOrWhiteSpace(peticion.Codigo) || string.IsNullOrWhiteSpace(peticion.Titulo))
        {
            return Result<LicitacionResponse>.Fallo(ErrorAplicacion.Validacion(
                CodigosError.MontoInvalido,
                "El código y el título de la licitación son obligatorios."));
        }

        if (await _licitaciones.ExisteCodigoAsync(peticion.Codigo, excluyendoId: null, cancelacion))
        {
            return Result<LicitacionResponse>.Fallo(CodigoDuplicado());
        }

        Licitacion licitacion;
        try
        {
            licitacion = Licitacion.Crear(
                peticion.Codigo,
                peticion.Titulo,
                peticion.PresupuestoEstimadoCRC,
                peticion.FechaCierre);
        }
        catch (MontoInvalidoException error)
        {
            return Result<LicitacionResponse>.Fallo(
                ErrorAplicacion.Validacion(CodigosError.MontoInvalido, error.Message));
        }

        _licitaciones.Agregar(licitacion);
        await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);

        return Result<LicitacionResponse>.Correcto(AResponse(licitacion, cantidadOfertas: 0));
    }

    /// <inheritdoc />
    public async Task<Result<LicitacionResponse>> ActualizarAsync(
        Guid id,
        ActualizarLicitacionRequest peticion,
        CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(peticion);

        var licitacion = await _licitaciones.ObtenerPorIdAsync(id, cancelacion);
        if (licitacion is null)
        {
            return Result<LicitacionResponse>.Fallo(NoEncontrada());
        }

        if (await _licitaciones.ExisteCodigoAsync(peticion.Codigo, id, cancelacion))
        {
            return Result<LicitacionResponse>.Fallo(CodigoDuplicado());
        }

        // El presupuesto no puede quedar por debajo de una oferta ya recibida: esa oferta
        // era válida cuando se presentó y no puede invalidarse retroactivamente.
        var ofertaMaxima = await _ofertas.ObtenerMontoMaximoAsync(id, cancelacion);
        if (ofertaMaxima is { } maxima && peticion.PresupuestoEstimadoCRC < maxima.Valor)
        {
            return Result<LicitacionResponse>.Fallo(ErrorAplicacion.Validacion(
                CodigosError.PresupuestoMenorQueOferta,
                string.Format(
                    CultureInfo.GetCultureInfo("es-CR"),
                    "El presupuesto no puede ser menor que la oferta más alta registrada ({0:N2} CRC).",
                    maxima.Valor)));
        }

        try
        {
            licitacion.CambiarPresupuesto(peticion.PresupuestoEstimadoCRC);
        }
        catch (MontoInvalidoException error)
        {
            return Result<LicitacionResponse>.Fallo(
                ErrorAplicacion.Validacion(CodigosError.MontoInvalido, error.Message));
        }

        licitacion.CambiarDatos(peticion.Codigo, peticion.Titulo, peticion.FechaCierre);
        await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);

        var cantidadOfertas = (await _ofertas.ObtenerPorLicitacionAsync(id, cancelacion)).Count;

        return Result<LicitacionResponse>.Correcto(AResponse(licitacion, cantidadOfertas));
    }

    /// <inheritdoc />
    public async Task<Result<LicitacionResponse>> ObtenerAsync(
        Guid id,
        CancellationToken cancelacion = default)
    {
        var licitacion = await _licitaciones.ObtenerPorIdAsync(id, cancelacion);
        if (licitacion is null)
        {
            return Result<LicitacionResponse>.Fallo(NoEncontrada());
        }

        var cantidadOfertas = (await _ofertas.ObtenerPorLicitacionAsync(id, cancelacion)).Count;

        return Result<LicitacionResponse>.Correcto(AResponse(licitacion, cantidadOfertas));
    }

    /// <inheritdoc />
    public async Task<Result<PagedResponse<LicitacionResponse>>> ListarAsync(
        ConsultaLicitaciones consulta,
        CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(consulta);

        var (elementos, total) = await _licitaciones.ListarAsync(
            consulta.Busqueda,
            consulta.Orden,
            consulta.Estado,
            consulta.Pagina,
            consulta.Tamano,
            cancelacion);

        var respuestas = elementos.Select(l => AResponse(l, cantidadOfertas: 0)).ToList();

        return Result<PagedResponse<LicitacionResponse>>.Correcto(
            new PagedResponse<LicitacionResponse>(respuestas, consulta.Pagina, consulta.Tamano, total));
    }

    /// <inheritdoc />
    public async Task<Result> EliminarAsync(Guid id, CancellationToken cancelacion = default)
    {
        var licitacion = await _licitaciones.ObtenerPorIdAsync(id, cancelacion);
        if (licitacion is null)
        {
            return Result.Fallo(NoEncontrada());
        }

        _licitaciones.Eliminar(licitacion);
        await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);

        return Result.Correcto();
    }

    /// <inheritdoc />
    public async Task<Result<LicitacionResponse>> CambiarEstadoAsync(
        Guid id,
        CambiarEstadoRequest peticion,
        CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(peticion);

        var licitacion = await _licitaciones.ObtenerPorIdAsync(id, cancelacion);
        if (licitacion is null)
        {
            return Result<LicitacionResponse>.Fallo(NoEncontrada());
        }

        // Publicar un proceso cuyo plazo ya venció dejaría una licitación que nace
        // cerrada funcionalmente y nunca podría recibir ofertas.
        if (peticion.Estado == EstadoLicitacion.Publicada
            && _reloj.UtcNow >= licitacion.FechaCierre.ToUniversalTime())
        {
            return Result<LicitacionResponse>.Fallo(ErrorAplicacion.Validacion(
                CodigosError.FechaCierreEnElPasado,
                "No se puede publicar una licitación cuya fecha de cierre ya pasó."));
        }

        try
        {
            licitacion.CambiarEstado(peticion.Estado);
        }
        catch (TransicionEstadoInvalidaException error)
        {
            return Result<LicitacionResponse>.Fallo(
                ErrorAplicacion.Conflicto(CodigosError.TransicionInvalida, error.Message));
        }

        await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);

        var cantidadOfertas = (await _ofertas.ObtenerPorLicitacionAsync(id, cancelacion)).Count;

        return Result<LicitacionResponse>.Correcto(AResponse(licitacion, cantidadOfertas));
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<OfertaResponse>>> ObtenerOfertasAsync(
        Guid id,
        CancellationToken cancelacion = default)
    {
        var licitacion = await _licitaciones.ObtenerPorIdAsync(id, cancelacion);
        if (licitacion is null)
        {
            return Result<IReadOnlyList<OfertaResponse>>.Fallo(NoEncontrada());
        }

        var (elementos, _) = await _ofertas.ListarDetalleAsync(
            new ConsultaOfertas { LicitacionId = id, Tamano = ConsultaPaginada.TamanoMaximo, Orden = "monto:asc" },
            cancelacion);

        return Result<IReadOnlyList<OfertaResponse>>.Correcto(elementos);
    }

    /// <inheritdoc />
    public async Task<Result<MejorOfertaResponse>> ObtenerMejorOfertaAsync(
        Guid id,
        CancellationToken cancelacion = default)
    {
        var licitacion = await _licitaciones.ObtenerPorIdAsync(id, cancelacion);
        if (licitacion is null)
        {
            return Result<MejorOfertaResponse>.Fallo(NoEncontrada());
        }

        var ofertas = await _ofertas.ObtenerPorLicitacionAsync(id, cancelacion);
        var mejor = EvaluadorMejorOferta.Seleccionar(ofertas);
        var clasificacion = ClasificadorAhorro.Clasificar(licitacion.PresupuestoEstimado, mejor?.Monto);

        OfertaResponse? detalle = null;
        string? aprobador = null;
        if (mejor is not null)
        {
            detalle = await _ofertas.ObtenerDetalleAsync(mejor.Id, cancelacion);

            // El aprobador se pide al módulo de niveles, no se calcula aquí: la política
            // de autorización es suya y se resuelve consultando su tabla.
            var nivel = await _niveles.ObtenerAprobadorAsync(mejor.Monto.Valor, cancelacion);
            aprobador = nivel.EsCorrecto ? nivel.Valor!.Aprobador : null;
        }

        return Result<MejorOfertaResponse>.Correcto(new MejorOfertaResponse(
            id,
            licitacion.PresupuestoEstimado.Valor,
            detalle,
            clasificacion.PorcentajeAhorro,
            clasificacion.Etiqueta,
            aprobador));
    }

    private static ErrorAplicacion NoEncontrada() => ErrorAplicacion.NoEncontrado(
        CodigosError.LicitacionNoEncontrada,
        "La licitación solicitada no existe.");

    private static ErrorAplicacion CodigoDuplicado() => ErrorAplicacion.Conflicto(
        CodigosError.CodigoLicitacionDuplicado,
        "Ya existe una licitación con ese código.");

    private LicitacionResponse AResponse(Licitacion licitacion, int cantidadOfertas) => new(
        licitacion.Id,
        licitacion.Codigo,
        licitacion.Titulo,
        licitacion.Estado,
        licitacion.PresupuestoEstimado.Valor,
        licitacion.FechaCierre,
        licitacion.EstaCerradaFuncionalmente(_reloj),
        cantidadOfertas,
        licitacion.CreatedAt,
        licitacion.UpdatedAt);
}
