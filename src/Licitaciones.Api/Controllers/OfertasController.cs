using Asp.Versioning;
using Licitaciones.Api.Comun;
using Licitaciones.Application.Comun;
using Licitaciones.Application.Ofertas;
using Microsoft.AspNetCore.Mvc;

namespace Licitaciones.Api.Controllers;

/// <summary>Administración de ofertas económicas.</summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ofertas")]
[Produces("application/json")]
public sealed class OfertasController : ControladorApiBase
{
    private readonly IOfertaService _ofertas;

    /// <summary>Crea el controlador.</summary>
    /// <param name="ofertas">Casos de uso de ofertas.</param>
    public OfertasController(IOfertaService ofertas) => _ofertas = ofertas;

    /// <summary>Lista las ofertas, con filtro por licitación y por proveedor.</summary>
    /// <param name="consulta">Página, tamaño, orden y filtros.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpGet]
    [ProducesResponseType<PagedResponse<OfertaResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<OfertaResponse>>> Listar(
        [FromQuery] ConsultaOfertas consulta,
        CancellationToken cancelacion)
    {
        var resultado = await _ofertas.ListarAsync(consulta, cancelacion);

        return Ok(resultado.Valor);
    }

    /// <summary>Consulta una oferta.</summary>
    /// <param name="id">Oferta consultada.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<OfertaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OfertaResponse>> Obtener(Guid id, CancellationToken cancelacion)
    {
        var resultado = await _ofertas.ObtenerAsync(id, cancelacion);

        return resultado.EsCorrecto ? Ok(resultado.Valor) : AProblema(resultado.Error!);
    }

    /// <summary>Registra una oferta.</summary>
    /// <param name="peticion">Datos de la oferta.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpPost]
    [ProducesResponseType<OfertaResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<OfertaResponse>> Crear(
        CrearOfertaRequest peticion,
        CancellationToken cancelacion)
    {
        var resultado = await _ofertas.CrearAsync(peticion, cancelacion);
        if (!resultado.EsCorrecto)
        {
            return AProblema(resultado.Error!);
        }

        return CreatedAtAction(
            nameof(Obtener),
            new { id = resultado.Valor!.Id, version = "1.0" },
            resultado.Valor);
    }

    /// <summary>Modifica el monto de una oferta.</summary>
    /// <param name="id">Oferta que se modifica.</param>
    /// <param name="peticion">Nuevo monto.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<OfertaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<OfertaResponse>> Actualizar(
        Guid id,
        ActualizarOfertaRequest peticion,
        CancellationToken cancelacion)
    {
        var resultado = await _ofertas.ActualizarAsync(id, peticion, cancelacion);

        return resultado.EsCorrecto ? Ok(resultado.Valor) : AProblema(resultado.Error!);
    }

    /// <summary>Elimina una oferta, si su licitación sigue vigente.</summary>
    /// <param name="id">Oferta que se elimina.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancelacion)
    {
        var resultado = await _ofertas.EliminarAsync(id, cancelacion);

        return resultado.EsCorrecto ? NoContent() : AProblema(resultado.Error!);
    }
}
