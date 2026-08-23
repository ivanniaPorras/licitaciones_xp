using Asp.Versioning;
using Licitaciones.Api.Comun;
using Licitaciones.Application.Comun;
using Licitaciones.Application.Moneda;
using Microsoft.AspNetCore.Mvc;

namespace Licitaciones.Api.Controllers;

/// <summary>
/// Administración de las tasas de cambio y lectura de montos en dólares. El sistema nunca
/// consulta un servicio externo de tasas: la vigente la administra una persona de la
/// organización.
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tipos-cambio")]
[Produces("application/json")]
public sealed class TiposCambioController : ControladorApiBase
{
    private readonly ITipoCambioService _tiposCambio;
    private readonly IConversionMonedaService _conversion;

    /// <summary>Crea el controlador.</summary>
    /// <param name="tiposCambio">Casos de uso de tipos de cambio.</param>
    /// <param name="conversion">Conversión de montos a dólares.</param>
    public TiposCambioController(
        ITipoCambioService tiposCambio,
        IConversionMonedaService conversion)
    {
        _tiposCambio = tiposCambio;
        _conversion = conversion;
    }

    /// <summary>Lista las tasas con paginación, filtrado y ordenamiento.</summary>
    /// <param name="consulta">Página, tamaño, orden y año de vigencia buscado.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpGet]
    [ProducesResponseType<PagedResponse<TipoCambioResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<TipoCambioResponse>>> Listar(
        [FromQuery] ConsultaTiposCambio consulta,
        CancellationToken cancelacion)
    {
        var resultado = await _tiposCambio.ListarAsync(consulta, cancelacion);

        return Ok(resultado.Valor);
    }

    /// <summary>Devuelve la tasa que el sistema está usando para convertir.</summary>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpGet("vigente")]
    [ProducesResponseType<TipoCambioResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TipoCambioResponse>> Vigente(CancellationToken cancelacion)
    {
        var resultado = await _tiposCambio.ObtenerVigenteAsync(cancelacion);

        return resultado.EsCorrecto ? Ok(resultado.Valor) : AProblema(resultado.Error!);
    }

    /// <summary>Expresa en dólares un monto en colones, con la tasa usada y su fecha.</summary>
    /// <param name="monto">Monto en colones que se quiere leer en dólares.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpGet("conversion")]
    [ProducesResponseType<ConversionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ConversionResponse>> Conversion(
        [FromQuery] decimal monto,
        CancellationToken cancelacion)
    {
        var resultado = await _conversion.ConvertirAsync(monto, cancelacion);

        return resultado.EsCorrecto ? Ok(resultado.Valor) : AProblema(resultado.Error!);
    }

    /// <summary>Consulta una tasa.</summary>
    /// <param name="id">Tasa consultada.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<TipoCambioResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TipoCambioResponse>> Obtener(
        Guid id,
        CancellationToken cancelacion)
    {
        var resultado = await _tiposCambio.ObtenerAsync(id, cancelacion);

        return resultado.EsCorrecto ? Ok(resultado.Valor) : AProblema(resultado.Error!);
    }

    /// <summary>Registra una tasa, que queda fuera de uso hasta que se active.</summary>
    /// <param name="peticion">Datos de la tasa.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpPost]
    [ProducesResponseType<TipoCambioResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<TipoCambioResponse>> Crear(
        CrearTipoCambioRequest peticion,
        CancellationToken cancelacion)
    {
        var resultado = await _tiposCambio.CrearAsync(peticion, cancelacion);
        if (!resultado.EsCorrecto)
        {
            return AProblema(resultado.Error!);
        }

        return CreatedAtAction(
            nameof(Obtener),
            new { id = resultado.Valor!.Id, version = "1.0" },
            resultado.Valor);
    }

    /// <summary>Modifica la tasa y su fecha de vigencia.</summary>
    /// <param name="id">Tasa que se modifica.</param>
    /// <param name="peticion">Nuevos datos.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<TipoCambioResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<TipoCambioResponse>> Actualizar(
        Guid id,
        ActualizarTipoCambioRequest peticion,
        CancellationToken cancelacion)
    {
        var resultado = await _tiposCambio.ActualizarAsync(id, peticion, cancelacion);

        return resultado.EsCorrecto ? Ok(resultado.Valor) : AProblema(resultado.Error!);
    }

    /// <summary>Pone la tasa en uso y retira de uso a la que lo estuviera.</summary>
    /// <param name="id">Tasa que pasa a estar vigente.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpPost("{id:guid}/activar")]
    [ProducesResponseType<TipoCambioResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TipoCambioResponse>> Activar(
        Guid id,
        CancellationToken cancelacion)
    {
        var resultado = await _tiposCambio.ActivarAsync(id, cancelacion);

        return resultado.EsCorrecto ? Ok(resultado.Valor) : AProblema(resultado.Error!);
    }

    /// <summary>Elimina una tasa.</summary>
    /// <param name="id">Tasa que se elimina.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancelacion)
    {
        var resultado = await _tiposCambio.EliminarAsync(id, cancelacion);

        return resultado.EsCorrecto ? NoContent() : AProblema(resultado.Error!);
    }
}
