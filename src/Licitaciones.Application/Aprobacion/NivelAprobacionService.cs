using Licitaciones.Application.Comun;
using Licitaciones.Application.Persistencia;
using Licitaciones.Domain.Aprobacion;
using Licitaciones.Domain.Dinero;

namespace Licitaciones.Application.Aprobacion;

/// <inheritdoc cref="INivelAprobacionService" />
public sealed class NivelAprobacionService : INivelAprobacionService
{
    private readonly INivelAprobacionRepository _niveles;
    private readonly IUnitOfWork _unidadDeTrabajo;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    /// <param name="niveles">Acceso a los niveles de aprobación.</param>
    /// <param name="unidadDeTrabajo">Confirmación de los cambios y transacciones.</param>
    public NivelAprobacionService(INivelAprobacionRepository niveles, IUnitOfWork unidadDeTrabajo)
    {
        _niveles = niveles;
        _unidadDeTrabajo = unidadDeTrabajo;
    }

    /// <inheritdoc />
    public async Task<Result<NivelAprobacionResponse>> ObtenerAprobadorAsync(
        decimal montoCRC,
        CancellationToken cancelacion = default)
    {
        MontoCRC monto;
        try
        {
            monto = MontoCRC.Crear(montoCRC);
        }
        catch (MontoInvalidoException error)
        {
            return Result<NivelAprobacionResponse>.Fallo(
                ErrorAplicacion.Validacion(CodigosError.MontoInvalido, error.Message));
        }

        // La consulta va contra la tabla: cambiar la política de aprobación es editar
        // filas, no recompilar el programa.
        var nivel = await _niveles.ObtenerAplicableAsync(monto, cancelacion);

        return nivel is null
            ? Result<NivelAprobacionResponse>.Fallo(ErrorAplicacion.Validacion(
                CodigosError.SinNivelAplicable,
                "Ningún nivel de aprobación cubre ese monto."))
            : Result<NivelAprobacionResponse>.Correcto(AResponse(nivel));
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<NivelAprobacionResponse>>> ListarAsync(
        CancellationToken cancelacion = default)
    {
        var niveles = await _niveles.ObtenerTodosAsync(cancelacion);

        return Result<IReadOnlyList<NivelAprobacionResponse>>.Correcto(
            [.. niveles.Select(AResponse)]);
    }

    /// <inheritdoc />
    public async Task<Result<NivelAprobacionResponse>> ObtenerAsync(
        Guid id,
        CancellationToken cancelacion = default)
    {
        var nivel = await _niveles.ObtenerPorIdAsync(id, cancelacion);

        return nivel is null
            ? Result<NivelAprobacionResponse>.Fallo(NoEncontrado())
            : Result<NivelAprobacionResponse>.Correcto(AResponse(nivel));
    }

    /// <inheritdoc />
    public async Task<Result<NivelAprobacionResponse>> CrearAsync(
        CrearNivelAprobacionRequest peticion,
        CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(peticion);

        // Toda la operación va en transacción: entre comprobar el traslape y guardar no
        // puede colarse otro rango que invalide la comprobación.
        return await _unidadDeTrabajo.EjecutarEnTransaccionAsync(async ct =>
        {
            var (nivel, error) = ConstruirNivel(
                peticion.MontoMinimoCRC,
                peticion.MontoMaximoCRC,
                peticion.Aprobador);

            if (error is not null)
            {
                return Result<NivelAprobacionResponse>.Fallo(error);
            }

            var existentes = await _niveles.ObtenerTodosAsync(ct);
            if (ComprobarConvivencia(nivel!, existentes, excluyendoId: null) is { } conflicto)
            {
                return Result<NivelAprobacionResponse>.Fallo(conflicto);
            }

            _niveles.Agregar(nivel!);

            return Result<NivelAprobacionResponse>.Correcto(AResponse(nivel!));
        }, cancelacion);
    }

    /// <inheritdoc />
    public async Task<Result<NivelAprobacionResponse>> ActualizarAsync(
        Guid id,
        ActualizarNivelAprobacionRequest peticion,
        CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(peticion);

        return await _unidadDeTrabajo.EjecutarEnTransaccionAsync(async ct =>
        {
            var actual = await _niveles.ObtenerPorIdAsync(id, ct);
            if (actual is null)
            {
                return Result<NivelAprobacionResponse>.Fallo(NoEncontrado());
            }

            var (propuesto, error) = ConstruirNivel(
                peticion.MontoMinimoCRC,
                peticion.MontoMaximoCRC,
                peticion.Aprobador);

            if (error is not null)
            {
                return Result<NivelAprobacionResponse>.Fallo(error);
            }

            var existentes = await _niveles.ObtenerTodosAsync(ct);
            if (ComprobarConvivencia(propuesto!, existentes, excluyendoId: id) is { } conflicto)
            {
                return Result<NivelAprobacionResponse>.Fallo(conflicto);
            }

            actual.CambiarRango(peticion.MontoMinimoCRC, peticion.MontoMaximoCRC, peticion.Aprobador);

            return Result<NivelAprobacionResponse>.Correcto(AResponse(actual));
        }, cancelacion);
    }

    /// <inheritdoc />
    public async Task<Result> EliminarAsync(Guid id, CancellationToken cancelacion = default)
    {
        var nivel = await _niveles.ObtenerPorIdAsync(id, cancelacion);
        if (nivel is null)
        {
            return Result.Fallo(NoEncontrado());
        }

        _niveles.Eliminar(nivel);
        await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);

        return Result.Correcto();
    }

    // Construye el nivel traduciendo los errores del dominio a errores de aplicación.
    private static (NivelAprobacion? Nivel, ErrorAplicacion? Error) ConstruirNivel(
        decimal montoMinimoCRC,
        decimal? montoMaximoCRC,
        string aprobador)
    {
        try
        {
            return (NivelAprobacion.Crear(montoMinimoCRC, montoMaximoCRC, aprobador), null);
        }
        catch (RangoAprobacionInvalidoException error)
        {
            return (null, ErrorAplicacion.Validacion(CodigosError.RangoInvalido, error.Message));
        }
        catch (MontoInvalidoException error)
        {
            return (null, ErrorAplicacion.Validacion(CodigosError.MontoInvalido, error.Message));
        }
    }

    /// <summary>
    /// Comprueba que el nivel pueda convivir con los ya existentes: ni se traslapa con
    /// ninguno, ni añade un segundo rango sin monto máximo.
    /// </summary>
    private static ErrorAplicacion? ComprobarConvivencia(
        NivelAprobacion candidato,
        IReadOnlyList<NivelAprobacion> existentes,
        Guid? excluyendoId)
    {
        // Al editar, el propio nivel no cuenta: si no se excluyera, cualquier rango se
        // traslaparía consigo mismo y sería imposible cambiarle el aprobador.
        var otros = existentes.Where(n => excluyendoId is null || n.Id != excluyendoId).ToList();

        if (candidato.EsRangoAbierto && otros.Any(n => n.EsRangoAbierto))
        {
            return ErrorAplicacion.Conflicto(
                CodigosError.RangoAbiertoDuplicado,
                "Ya existe un nivel sin monto máximo.");
        }

        return otros.Any(candidato.SeTraslapaCon)
            ? ErrorAplicacion.Conflicto(
                CodigosError.RangoTraslapado,
                "El rango se traslapa con un nivel existente.")
            : null;
    }

    private static ErrorAplicacion NoEncontrado() => ErrorAplicacion.NoEncontrado(
        CodigosError.NivelAprobacionNoEncontrado,
        "El nivel de aprobación solicitado no existe.");

    private static NivelAprobacionResponse AResponse(NivelAprobacion nivel) => new(
        nivel.Id,
        nivel.MontoMinimo.Valor,
        nivel.MontoMaximo?.Valor,
        nivel.EsRangoAbierto,
        nivel.Aprobador);
}
