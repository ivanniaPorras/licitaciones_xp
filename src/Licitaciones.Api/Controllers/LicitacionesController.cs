using Asp.Versioning;
using Licitaciones.Api.Comun;
using Licitaciones.Application.Comun;
using Licitaciones.Application.Licitaciones;
using Licitaciones.Application.Ofertas;
using Microsoft.AspNetCore.Mvc;

namespace Licitaciones.Api.Controllers;

/// <summary>Administración de licitaciones.</summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/licitaciones")]
[Produces("application/json")]
public sealed class LicitacionesController : ControladorApiBase
{
    private readonly ILicitacionService _licitaciones;
    private readonly IOfertaService _ofertas;

    /// <summary>Crea el controlador.</summary>
    /// <param name="licitaciones">Casos de uso de licitaciones.</param>
    /// <param name="ofertas">Casos de uso de ofertas.</param>
    public LicitacionesController(ILicitacionService licitaciones, IOfertaService ofertas)
    {
        _licitaciones = licitaciones;
        _ofertas = ofertas;
    }

    /// <summary>Lista las licitaciones con paginación, filtrado y ordenamiento.</summary>
    /// <param name="consulta">Página, tamaño, orden, búsqueda y estado.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpGet]
    [ProducesResponseType<PagedResponse<LicitacionResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<LicitacionResponse>>> Listar(
        [FromQuery] ConsultaLicitaciones consulta,
        CancellationToken cancelacion)
    {
        var resultado = await _licitaciones.ListarAsync(consulta, cancelacion);

        return Ok(resultado.Valor);
    }

    /// <summary>Consulta una licitación.</summary>
    /// <param name="id">Licitación consultada.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<LicitacionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LicitacionResponse>> Obtener(Guid id, CancellationToken cancelacion)
    {
        var resultado = await _licitaciones.ObtenerAsync(id, cancelacion);

        return resultado.EsCorrecto ? Ok(resultado.Valor) : AProblema(resultado.Error!);
    }

    /// <summary>Registra una licitación en estado Borrador.</summary>
    /// <param name="peticion">Datos de la licitación.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpPost]
    [ProducesResponseType<LicitacionResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<LicitacionResponse>> Crear(
        CrearLicitacionRequest peticion,
        CancellationToken cancelacion)
    {
        var resultado = await _licitaciones.CrearAsync(peticion, cancelacion);
        if (!resultado.EsCorrecto)
        {
            return AProblema(resultado.Error!);
        }

        return CreatedAtAction(
            nameof(Obtener),
            new { id = resultado.Valor!.Id, version = "1.0" },
            resultado.Valor);
    }

    /// <summary>Modifica una licitación.</summary>
    /// <param name="id">Licitación que se modifica.</param>
    /// <param name="peticion">Nuevos datos.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<LicitacionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<LicitacionResponse>> Actualizar(
        Guid id,
        ActualizarLicitacionRequest peticion,
        CancellationToken cancelacion)
    {
        var resultado = await _licitaciones.ActualizarAsync(id, peticion, cancelacion);

        return resultado.EsCorrecto ? Ok(resultado.Valor) : AProblema(resultado.Error!);
    }

    /// <summary>Cambia el estado de una licitación.</summary>
    /// <param name="id">Licitación que cambia de estado.</param>
    /// <param name="peticion">Estado destino.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpPatch("{id:guid}/estado")]
    [ProducesResponseType<LicitacionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<LicitacionResponse>> CambiarEstado(
        Guid id,
        CambiarEstadoRequest peticion,
        CancellationToken cancelacion)
    {
        var resultado = await _licitaciones.CambiarEstadoAsync(id, peticion, cancelacion);

        return resultado.EsCorrecto ? Ok(resultado.Valor) : AProblema(resultado.Error!);
    }

    /// <summary>Da de baja una licitación. El borrado es lógico.</summary>
    /// <param name="id">Licitación que se da de baja.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancelacion)
    {
        var resultado = await _licitaciones.EliminarAsync(id, cancelacion);

        return resultado.EsCorrecto ? NoContent() : AProblema(resultado.Error!);
    }

    /// <summary>Consulta las ofertas recibidas por una licitación.</summary>
    /// <param name="id">Licitación consultada.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpGet("{id:guid}/ofertas")]
    [ProducesResponseType<IReadOnlyList<OfertaResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<OfertaResponse>>> Ofertas(
        Guid id,
        CancellationToken cancelacion)
    {
        var resultado = await _licitaciones.ObtenerOfertasAsync(id, cancelacion);

        return resultado.EsCorrecto ? Ok(resultado.Valor) : AProblema(resultado.Error!);
    }

    /// <summary>Registra una oferta sobre esta licitación.</summary>
    /// <param name="id">Licitación a la que se presenta.</param>
    /// <param name="peticion">Proveedor y monto.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpPost("{id:guid}/ofertas")]
    [ProducesResponseType<OfertaResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<OfertaResponse>> CrearOferta(
        Guid id,
        CrearOfertaEnLicitacionRequest peticion,
        CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(peticion);

        // La licitación viene de la ruta, no del cuerpo: la ruta es la fuente de verdad.
        var resultado = await _ofertas.CrearAsync(
            new CrearOfertaRequest(id, peticion.ProveedorId, peticion.MontoOfertadoCRC),
            cancelacion);

        if (!resultado.EsCorrecto)
        {
            return AProblema(resultado.Error!);
        }

        return Created($"/api/v1/ofertas/{resultado.Valor!.Id}", resultado.Valor);
    }

    /// <summary>Consulta la mejor oferta con su ahorro y su clasificación.</summary>
    /// <param name="id">Licitación consultada.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpGet("{id:guid}/mejor-oferta")]
    [ProducesResponseType<MejorOfertaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MejorOfertaResponse>> MejorOferta(
        Guid id,
        CancellationToken cancelacion)
    {
        var resultado = await _licitaciones.ObtenerMejorOfertaAsync(id, cancelacion);

        return resultado.EsCorrecto ? Ok(resultado.Valor) : AProblema(resultado.Error!);
    }
}
