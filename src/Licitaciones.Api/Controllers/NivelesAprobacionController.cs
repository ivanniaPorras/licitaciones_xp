using Asp.Versioning;
using Licitaciones.Api.Comun;
using Licitaciones.Application.Aprobacion;
using Microsoft.AspNetCore.Mvc;

namespace Licitaciones.Api.Controllers;

/// <summary>
/// Administración de los niveles de aprobación. La política de autorización vive en esta
/// tabla, de modo que puede cambiarse sin modificar el programa.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/niveles-aprobacion")]
[Produces("application/json")]
public sealed class NivelesAprobacionController : ControladorApiBase
{
    private readonly INivelAprobacionService _niveles;

    /// <summary>Crea el controlador.</summary>
    /// <param name="niveles">Casos de uso de niveles de aprobación.</param>
    public NivelesAprobacionController(INivelAprobacionService niveles) => _niveles = niveles;

    /// <summary>Lista los niveles ordenados por su monto mínimo.</summary>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<NivelAprobacionResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<NivelAprobacionResponse>>> Listar(
        CancellationToken cancelacion)
    {
        var resultado = await _niveles.ListarAsync(cancelacion);

        return Ok(resultado.Valor);
    }

    /// <summary>Consulta un nivel de aprobación.</summary>
    /// <param name="id">Nivel consultado.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<NivelAprobacionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NivelAprobacionResponse>> Obtener(
        Guid id,
        CancellationToken cancelacion)
    {
        var resultado = await _niveles.ObtenerAsync(id, cancelacion);

        return resultado.EsCorrecto ? Ok(resultado.Valor) : AProblema(resultado.Error!);
    }

    /// <summary>Determina quién debe aprobar el monto indicado.</summary>
    /// <param name="monto">Monto en colones que se quiere aprobar.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpGet("aprobador")]
    [ProducesResponseType<NivelAprobacionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<NivelAprobacionResponse>> Aprobador(
        [FromQuery] decimal monto,
        CancellationToken cancelacion)
    {
        var resultado = await _niveles.ObtenerAprobadorAsync(monto, cancelacion);

        return resultado.EsCorrecto ? Ok(resultado.Valor) : AProblema(resultado.Error!);
    }

    /// <summary>Registra un nivel de aprobación.</summary>
    /// <param name="peticion">Datos del nivel.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpPost]
    [ProducesResponseType<NivelAprobacionResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<NivelAprobacionResponse>> Crear(
        CrearNivelAprobacionRequest peticion,
        CancellationToken cancelacion)
    {
        var resultado = await _niveles.CrearAsync(peticion, cancelacion);
        if (!resultado.EsCorrecto)
        {
            return AProblema(resultado.Error!);
        }

        return CreatedAtAction(
            nameof(Obtener),
            new { id = resultado.Valor!.Id, version = "1.0" },
            resultado.Valor);
    }

    /// <summary>Modifica un nivel de aprobación.</summary>
    /// <param name="id">Nivel que se modifica.</param>
    /// <param name="peticion">Nuevos datos.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<NivelAprobacionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<NivelAprobacionResponse>> Actualizar(
        Guid id,
        ActualizarNivelAprobacionRequest peticion,
        CancellationToken cancelacion)
    {
        var resultado = await _niveles.ActualizarAsync(id, peticion, cancelacion);

        return resultado.EsCorrecto ? Ok(resultado.Valor) : AProblema(resultado.Error!);
    }

    /// <summary>Elimina un nivel de aprobación.</summary>
    /// <param name="id">Nivel que se elimina.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancelacion)
    {
        var resultado = await _niveles.EliminarAsync(id, cancelacion);

        return resultado.EsCorrecto ? NoContent() : AProblema(resultado.Error!);
    }
}
