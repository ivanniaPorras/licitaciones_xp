using Licitaciones.Application.Comun;
using Licitaciones.Application.Persistencia;
using Licitaciones.Domain.Dinero;
using Licitaciones.Domain.Licitaciones;
using Licitaciones.Domain.Ofertas;
using Licitaciones.Domain.Tiempo;

namespace Licitaciones.Application.Ofertas;

/// <inheritdoc cref="IOfertaService" />
public sealed class OfertaService : IOfertaService
{
    private readonly IOfertaRepository _ofertas;
    private readonly ILicitacionRepository _licitaciones;
    private readonly IProveedorRepository _proveedores;
    private readonly IUnitOfWork _unidadDeTrabajo;
    private readonly IClock _reloj;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    /// <param name="ofertas">Acceso a las ofertas.</param>
    /// <param name="licitaciones">Acceso a las licitaciones.</param>
    /// <param name="proveedores">Acceso a los proveedores.</param>
    /// <param name="unidadDeTrabajo">Confirmación de los cambios.</param>
    /// <param name="reloj">Reloj del que se toma el instante actual.</param>
    public OfertaService(
        IOfertaRepository ofertas,
        ILicitacionRepository licitaciones,
        IProveedorRepository proveedores,
        IUnitOfWork unidadDeTrabajo,
        IClock reloj)
    {
        _ofertas = ofertas;
        _licitaciones = licitaciones;
        _proveedores = proveedores;
        _unidadDeTrabajo = unidadDeTrabajo;
        _reloj = reloj;
    }

    /// <inheritdoc />
    public async Task<Result<OfertaResponse>> CrearAsync(
        CrearOfertaRequest peticion,
        CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(peticion);

        var licitacion = await _licitaciones.ObtenerPorIdAsync(peticion.LicitacionId, cancelacion);
        if (licitacion is null)
        {
            return Result<OfertaResponse>.Fallo(ErrorAplicacion.NoEncontrado(
                CodigosError.LicitacionNoEncontrada,
                "La licitación solicitada no existe."));
        }

        var proveedor = await _proveedores.ObtenerPorIdAsync(peticion.ProveedorId, cancelacion);
        if (proveedor is null)
        {
            return Result<OfertaResponse>.Fallo(ErrorAplicacion.NoEncontrado(
                CodigosError.ProveedorNoEncontrado,
                "El proveedor solicitado no existe."));
        }

        if (ComprobarQueAdmiteOfertas(licitacion) is { } rechazo)
        {
            return Result<OfertaResponse>.Fallo(rechazo);
        }

        if (await _ofertas.ExisteOfertaDelProveedorAsync(licitacion.Id, proveedor.Id, cancelacion))
        {
            return Result<OfertaResponse>.Fallo(ErrorAplicacion.Conflicto(
                CodigosError.OfertaDuplicada,
                "Este proveedor ya registró una oferta para esta licitación."));
        }

        if (ComprobarMonto(peticion.MontoOfertadoCRC, licitacion) is { } montoInvalido)
        {
            return Result<OfertaResponse>.Fallo(montoInvalido);
        }

        // La fecha de registro sale del reloj inyectado, no del cliente: es la que define
        // el orden de llegada y el desempate de la mejor oferta.
        var oferta = Oferta.Crear(
            licitacion.Id,
            proveedor.Id,
            peticion.MontoOfertadoCRC,
            _reloj.UtcNow);

        _ofertas.Agregar(oferta);
        await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);

        return Result<OfertaResponse>.Correcto(
            new OfertaResponse(
                oferta.Id,
                licitacion.Id,
                licitacion.Codigo,
                proveedor.Id,
                proveedor.Nombre,
                oferta.Monto.Valor,
                oferta.FechaRegistro));
    }

    /// <inheritdoc />
    public async Task<Result<OfertaResponse>> ActualizarAsync(
        Guid id,
        ActualizarOfertaRequest peticion,
        CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(peticion);

        var (oferta, licitacion, error) = await CargarParaModificarAsync(id, cancelacion);
        if (error is not null)
        {
            return Result<OfertaResponse>.Fallo(error);
        }

        if (ComprobarMonto(peticion.MontoOfertadoCRC, licitacion!) is { } montoInvalido)
        {
            return Result<OfertaResponse>.Fallo(montoInvalido);
        }

        oferta!.CambiarMonto(peticion.MontoOfertadoCRC);
        await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);

        var detalle = await _ofertas.ObtenerDetalleAsync(id, cancelacion);

        return Result<OfertaResponse>.Correcto(detalle ?? new OfertaResponse(
            oferta.Id,
            licitacion!.Id,
            licitacion.Codigo,
            oferta.ProveedorId,
            string.Empty,
            oferta.Monto.Valor,
            oferta.FechaRegistro));
    }

    /// <inheritdoc />
    public async Task<Result> EliminarAsync(Guid id, CancellationToken cancelacion = default)
    {
        var (oferta, _, error) = await CargarParaModificarAsync(id, cancelacion);
        if (error is not null)
        {
            return Result.Fallo(error);
        }

        _ofertas.Eliminar(oferta!);
        await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);

        return Result.Correcto();
    }

    /// <inheritdoc />
    public async Task<Result<OfertaResponse>> ObtenerAsync(
        Guid id,
        CancellationToken cancelacion = default)
    {
        var detalle = await _ofertas.ObtenerDetalleAsync(id, cancelacion);

        return detalle is null
            ? Result<OfertaResponse>.Fallo(NoEncontrada())
            : Result<OfertaResponse>.Correcto(detalle);
    }

    /// <inheritdoc />
    public async Task<Result<PagedResponse<OfertaResponse>>> ListarAsync(
        ConsultaOfertas consulta,
        CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(consulta);

        var (elementos, total) = await _ofertas.ListarDetalleAsync(consulta, cancelacion);

        return Result<PagedResponse<OfertaResponse>>.Correcto(
            new PagedResponse<OfertaResponse>(elementos, consulta.Pagina, consulta.Tamano, total));
    }

    /// <summary>
    /// Carga la oferta y su licitación comprobando que la licitación siga admitiendo
    /// cambios. Editar y eliminar comparten exactamente la misma condición, así que la
    /// comprobación vive en un solo sitio.
    /// </summary>
    private async Task<(Oferta? Oferta, Licitacion? Licitacion, ErrorAplicacion? Error)>
        CargarParaModificarAsync(Guid id, CancellationToken cancelacion)
    {
        var oferta = await _ofertas.ObtenerPorIdAsync(id, cancelacion);
        if (oferta is null)
        {
            return (null, null, NoEncontrada());
        }

        var licitacion = await _licitaciones.ObtenerPorIdAsync(oferta.LicitacionId, cancelacion);
        if (licitacion is null)
        {
            return (null, null, NoEncontrada());
        }

        // Una oferta de una licitación cerrada funcionalmente es evidencia del proceso:
        // no se edita ni se elimina, aunque el estado almacenado siga diciendo Publicada.
        if (licitacion.EstaCerradaFuncionalmente(_reloj))
        {
            return (null, null, ErrorAplicacion.Conflicto(
                CodigosError.OfertaInmutable,
                "Las ofertas de licitaciones cerradas no pueden modificarse."));
        }

        return (oferta, licitacion, null);
    }

    private ErrorAplicacion? ComprobarQueAdmiteOfertas(Licitacion licitacion)
    {
        // El orden importa: una licitación en Borrador todavía no publicada da un mensaje
        // distinto al de una publicada cuyo plazo venció.
        if (licitacion.Estado == EstadoLicitacion.Borrador)
        {
            return ErrorAplicacion.Conflicto(
                CodigosError.LicitacionNoPublicada,
                "La licitación no está publicada.");
        }

        if (licitacion.EstaCerradaFuncionalmente(_reloj))
        {
            return ErrorAplicacion.Conflicto(
                CodigosError.LicitacionCerrada,
                "La licitación ya cerró; no se admiten más ofertas.");
        }

        return null;
    }

    private static ErrorAplicacion? ComprobarMonto(decimal monto, Licitacion licitacion)
    {
        MontoCRC montoOfertado;
        try
        {
            montoOfertado = MontoCRC.Crear(monto);
        }
        catch (MontoInvalidoException error)
        {
            return ErrorAplicacion.Validacion(CodigosError.MontoInvalido, error.Message);
        }

        // Una oferta igual al presupuesto es válida; solo se rechaza si lo supera.
        return montoOfertado > licitacion.PresupuestoEstimado
            ? ErrorAplicacion.Validacion(
                CodigosError.OfertaSuperaPresupuesto,
                "La oferta no puede superar el presupuesto de la licitación.")
            : null;
    }

    private static ErrorAplicacion NoEncontrada() => ErrorAplicacion.NoEncontrado(
        CodigosError.OfertaNoEncontrada,
        "La oferta solicitada no existe.");
}
