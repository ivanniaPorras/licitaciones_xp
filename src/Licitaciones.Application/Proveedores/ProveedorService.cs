using Licitaciones.Application.Comun;
using Licitaciones.Application.Ofertas;
using Licitaciones.Application.Persistencia;
using Licitaciones.Domain.Proveedores;

namespace Licitaciones.Application.Proveedores;

/// <inheritdoc cref="IProveedorService" />
public sealed class ProveedorService : IProveedorService
{
    private readonly IProveedorRepository _proveedores;
    private readonly IOfertaRepository _ofertas;
    private readonly IUnitOfWork _unidadDeTrabajo;

    /// <summary>Crea el servicio con sus dependencias.</summary>
    /// <param name="proveedores">Acceso a los proveedores.</param>
    /// <param name="ofertas">Acceso a las ofertas.</param>
    /// <param name="unidadDeTrabajo">Confirmación de los cambios.</param>
    public ProveedorService(
        IProveedorRepository proveedores,
        IOfertaRepository ofertas,
        IUnitOfWork unidadDeTrabajo)
    {
        _proveedores = proveedores;
        _ofertas = ofertas;
        _unidadDeTrabajo = unidadDeTrabajo;
    }

    /// <inheritdoc />
    public async Task<Result<ProveedorResponse>> CrearAsync(
        CrearProveedorRequest peticion,
        CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(peticion);

        if (!ValidadorNombreProveedor.EsValido(peticion.Nombre))
        {
            return Result<ProveedorResponse>.Fallo(ErrorAplicacion.Validacion(
                CodigosError.NombreProveedorInvalido,
                "El nombre solo admite letras, números, espacios, punto, coma y paréntesis."));
        }

        if (await _proveedores.ExisteNombreAsync(peticion.Nombre, excluyendoId: null, cancelacion))
        {
            return Result<ProveedorResponse>.Fallo(ErrorAplicacion.Conflicto(
                CodigosError.ProveedorDuplicado,
                "Ya existe un proveedor con ese nombre."));
        }

        var proveedor = Proveedor.Crear(peticion.Nombre);
        _proveedores.Agregar(proveedor);
        await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);

        return Result<ProveedorResponse>.Correcto(AResponse(proveedor, cantidadOfertas: 0));
    }

    /// <inheritdoc />
    public async Task<Result<ProveedorResponse>> ActualizarAsync(
        Guid id,
        ActualizarProveedorRequest peticion,
        CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(peticion);

        var proveedor = await _proveedores.ObtenerPorIdAsync(id, cancelacion);
        if (proveedor is null)
        {
            return Result<ProveedorResponse>.Fallo(NoEncontrado());
        }

        if (!ValidadorNombreProveedor.EsValido(peticion.Nombre))
        {
            return Result<ProveedorResponse>.Fallo(ErrorAplicacion.Validacion(
                CodigosError.NombreProveedorInvalido,
                "El nombre solo admite letras, números, espacios, punto, coma y paréntesis."));
        }

        // Se excluye el propio proveedor para que conservar su nombre no cuente como duplicado.
        if (await _proveedores.ExisteNombreAsync(peticion.Nombre, id, cancelacion))
        {
            return Result<ProveedorResponse>.Fallo(ErrorAplicacion.Conflicto(
                CodigosError.ProveedorDuplicado,
                "Ya existe un proveedor con ese nombre."));
        }

        proveedor.Renombrar(peticion.Nombre);
        await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);

        var cantidadOfertas = (await _ofertas.ObtenerPorProveedorAsync(id, cancelacion)).Count;

        return Result<ProveedorResponse>.Correcto(AResponse(proveedor, cantidadOfertas));
    }

    /// <inheritdoc />
    public async Task<Result<ProveedorResponse>> ObtenerAsync(
        Guid id,
        CancellationToken cancelacion = default)
    {
        var proveedor = await _proveedores.ObtenerPorIdAsync(id, cancelacion);
        if (proveedor is null)
        {
            return Result<ProveedorResponse>.Fallo(NoEncontrado());
        }

        var cantidadOfertas = (await _ofertas.ObtenerPorProveedorAsync(id, cancelacion)).Count;

        return Result<ProveedorResponse>.Correcto(AResponse(proveedor, cantidadOfertas));
    }

    /// <inheritdoc />
    public async Task<Result<PagedResponse<ProveedorResponse>>> ListarAsync(
        ConsultaProveedores consulta,
        CancellationToken cancelacion = default)
    {
        ArgumentNullException.ThrowIfNull(consulta);

        var (elementos, total) = await _proveedores.ListarAsync(
            consulta.Busqueda,
            consulta.Orden,
            consulta.Pagina,
            consulta.Tamano,
            cancelacion);

        var respuestas = new List<ProveedorResponse>(elementos.Count);
        foreach (var proveedor in elementos)
        {
            respuestas.Add(AResponse(proveedor, cantidadOfertas: 0));
        }

        return Result<PagedResponse<ProveedorResponse>>.Correcto(
            new PagedResponse<ProveedorResponse>(respuestas, consulta.Pagina, consulta.Tamano, total));
    }

    /// <inheritdoc />
    public async Task<Result> EliminarAsync(Guid id, CancellationToken cancelacion = default)
    {
        var proveedor = await _proveedores.ObtenerPorIdAsync(id, cancelacion);
        if (proveedor is null)
        {
            return Result.Fallo(NoEncontrado());
        }

        // No se comprueba si tiene ofertas: el borrado es lógico, de modo que la fila se
        // conserva y las ofertas que la referencian nunca quedan huérfanas.
        _proveedores.Eliminar(proveedor);
        await _unidadDeTrabajo.GuardarCambiosAsync(cancelacion);

        return Result.Correcto();
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<OfertaResponse>>> ObtenerOfertasAsync(
        Guid id,
        CancellationToken cancelacion = default)
    {
        var proveedor = await _proveedores.ObtenerPorIdAsync(id, cancelacion);
        if (proveedor is null)
        {
            return Result<IReadOnlyList<OfertaResponse>>.Fallo(NoEncontrado());
        }

        var (elementos, _) = await _ofertas.ListarDetalleAsync(
            new ConsultaOfertas { ProveedorId = id, Tamano = ConsultaPaginada.TamanoMaximo },
            cancelacion);

        return Result<IReadOnlyList<OfertaResponse>>.Correcto(elementos);
    }

    private static ErrorAplicacion NoEncontrado() => ErrorAplicacion.NoEncontrado(
        CodigosError.ProveedorNoEncontrado,
        "El proveedor solicitado no existe.");

    private static ProveedorResponse AResponse(Proveedor proveedor, int cantidadOfertas) =>
        new(proveedor.Id, proveedor.Nombre, cantidadOfertas, proveedor.CreatedAt, proveedor.UpdatedAt);
}
