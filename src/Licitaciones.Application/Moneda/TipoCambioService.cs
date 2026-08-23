using Licitaciones.Application.Comun;
using Licitaciones.Application.Persistencia;
using Licitaciones.Domain.Dinero;

namespace Licitaciones.Application.Moneda;

/// <inheritdoc cref="ITipoCambioService" />
public sealed class TipoCambioService : ITipoCambioService
{
    // Año que ninguna vigencia puede tener, para que una búsqueda que no es un año no
    // devuelva el listado completo.
    private const int AnioImposible = -1;

    private readonly ITipoCambioRepository _tiposCambio;
    private readonly IUnitOfWork _unidadDeTrabajo;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    /// <param name="tiposCambio">Acceso a los tipos de cambio.</param>
    /// <param name="unidadDeTrabajo">Confirmación de los cambios y transacciones.</param>
    public TipoCambioService(ITipoCambioRepository tiposCambio, IUnitOfWork unidadDeTrabajo)
    {
        _tiposCambio = tiposCambio;
        _unidadDeTrabajo = unidadDeTrabajo;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResponse<TipoCambioResponse>>> ListarAsync(
        ConsultaTiposCambio consulta,
        CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(consulta);

        var (elementos, total) = await _tiposCambio.ListarAsync(
            AnioDe(consulta.Busqueda),
            consulta.Orden,
            consulta.Pagina,
            consulta.Tamano,
            cancelacion);

        return Result<PagedResponse<TipoCambioResponse>>.Correcto(
            new PagedResponse<TipoCambioResponse>(
                [.. elementos.Select(AResponse)],
                consulta.Pagina,
                consulta.Tamano,
                total));
    }

    /// <inheritdoc />
    public async Task<Result<TipoCambioResponse>> ObtenerAsync(
        Guid id,
        CancellationToken cancelacion = default)
    {
        var tipoCambio = await _tiposCambio.ObtenerPorIdAsync(id, cancelacion);

        return tipoCambio is null
            ? Result<TipoCambioResponse>.Fallo(NoEncontrado())
            : Result<TipoCambioResponse>.Correcto(AResponse(tipoCambio));
    }

    /// <inheritdoc />
    public async Task<Result<TipoCambioResponse>> ObtenerVigenteAsync(
        CancellationToken cancelacion = default)
    {
        var vigente = await _tiposCambio.ObtenerActivoAsync(cancelacion);

        return vigente is null
            ? Result<TipoCambioResponse>.Fallo(SinTasaActiva())
            : Result<TipoCambioResponse>.Correcto(AResponse(vigente));
    }

    /// <inheritdoc />
    public async Task<Result<TipoCambioResponse>> CrearAsync(
        CrearTipoCambioRequest peticion,
        CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(peticion);

        TipoCambio tipoCambio;
        try
        {
            tipoCambio = TipoCambio.Crear(peticion.CRCporUSD, peticion.FechaVigencia);
        }
        catch (MontoInvalidoException error)
        {
            return Result<TipoCambioResponse>.Fallo(TasaInvalida(error));
        }

        // La tasa nace fuera de uso: ponerla a convertir es una decisión aparte, para que
        // registrarla no cambie sin querer todos los montos que ya se están mostrando.
        _tiposCambio.Agregar(tipoCambio);
        await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);

        return Result<TipoCambioResponse>.Correcto(AResponse(tipoCambio));
    }

    /// <inheritdoc />
    public async Task<Result<TipoCambioResponse>> ActualizarAsync(
        Guid id,
        ActualizarTipoCambioRequest peticion,
        CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(peticion);

        var tipoCambio = await _tiposCambio.ObtenerPorIdAsync(id, cancelacion);
        if (tipoCambio is null)
        {
            return Result<TipoCambioResponse>.Fallo(NoEncontrado());
        }

        try
        {
            tipoCambio.CambiarTasa(peticion.CRCporUSD, peticion.FechaVigencia);
        }
        catch (MontoInvalidoException error)
        {
            return Result<TipoCambioResponse>.Fallo(TasaInvalida(error));
        }

        await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);

        return Result<TipoCambioResponse>.Correcto(AResponse(tipoCambio));
    }

    /// <inheritdoc />
    public async Task<Result<TipoCambioResponse>> ActivarAsync(
        Guid id,
        CancellationToken cancelacion = default)
    {
        // Retirar la tasa anterior y poner la nueva ocurre dentro de una transacción. Si
        // el proceso se interrumpiera entre las dos operaciones, el sistema quedaría con
        // dos tasas activas o con ninguna, y ambos estados incumplen la regla.
        return await _unidadDeTrabajo.EjecutarEnTransaccionAsync(async ct =>
        {
            var destino = await _tiposCambio.ObtenerPorIdAsync(id, ct);
            if (destino is null)
            {
                return Result<TipoCambioResponse>.Fallo(NoEncontrado());
            }

            var vigente = await _tiposCambio.ObtenerActivoAsync(ct);
            if (vigente is not null && vigente.Id != destino.Id)
            {
                vigente.Desactivar();

                // El índice único parcial se comprueba en cada instrucción, no al cerrar
                // la transacción, así que la tasa anterior tiene que quedar retirada antes
                // de que la nueva se marque.
                await _unidadDeTrabajo.GuardarCambiosAsync(ct);
            }

            destino.Activar();

            return Result<TipoCambioResponse>.Correcto(AResponse(destino));
        }, cancelacion);
    }

    /// <inheritdoc />
    public async Task<Result> EliminarAsync(Guid id, CancellationToken cancelacion = default)
    {
        var tipoCambio = await _tiposCambio.ObtenerPorIdAsync(id, cancelacion);
        if (tipoCambio is null)
        {
            return Result.Fallo(NoEncontrado());
        }

        _tiposCambio.Eliminar(tipoCambio);
        await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);

        return Result.Correcto();
    }

    // El listado solo contiene números y fechas, así que la búsqueda se interpreta como
    // el año de vigencia. Un término que no sea un año deja el filtro sin coincidencias,
    // que es la respuesta correcta a una búsqueda que no encuentra nada.
    private static int? AnioDe(string? busqueda)
    {
        if (string.IsNullOrWhiteSpace(busqueda))
        {
            return null;
        }

        return int.TryParse(busqueda.Trim(), out var anio) ? anio : AnioImposible;
    }

    private static ErrorAplicacion NoEncontrado() => ErrorAplicacion.NoEncontrado(
        CodigosError.TipoCambioNoEncontrado,
        "El tipo de cambio solicitado no existe.");

    // Es un conflicto y no una validación: los datos de la petición están bien, lo que
    // falta es un estado del sistema que alguien debe corregir registrando una tasa.
    private static ErrorAplicacion SinTasaActiva() => ErrorAplicacion.Conflicto(
        CodigosError.SinTipoCambioActivo,
        "No hay un tipo de cambio activo para realizar la conversión.");

    private static ErrorAplicacion TasaInvalida(MontoInvalidoException error) =>
        ErrorAplicacion.Validacion(CodigosError.TasaInvalida, error.Message);

    private static TipoCambioResponse AResponse(TipoCambio tipoCambio) => new(
        tipoCambio.Id,
        tipoCambio.CRCporUSD,
        tipoCambio.FechaVigencia,
        tipoCambio.Activo,
        tipoCambio.CreatedAt,
        tipoCambio.UpdatedAt);
}
