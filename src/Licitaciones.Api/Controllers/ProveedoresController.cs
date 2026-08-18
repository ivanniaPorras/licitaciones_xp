using Asp.Versioning;
using Licitaciones.Api.Comun;
using Licitaciones.Application.Comun;
using Licitaciones.Application.Ofertas;
using Licitaciones.Application.Proveedores;
using Microsoft.AspNetCore.Mvc;

namespace Licitaciones.Api.Controllers;

/// <summary>Administración de proveedores.</summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/proveedores")]
[Produces("application/json")]
public sealed class ProveedoresController : ControladorApiBase
{
    private readonly IProveedorService _proveedores;

    /// <summary>Crea el controlador.</summary>
    /// <param name="proveedores">Casos de uso de proveedores.</param>
    public ProveedoresController(IProveedorService proveedores) => _proveedores = proveedores;

    /// <summary>Lista los proveedores con paginación, filtrado y ordenamiento.</summary>
    /// <param name="consulta">Página, tamaño, orden y búsqueda.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpGet]
    [ProducesResponseType<PagedResponse<ProveedorResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<ProveedorResponse>>> Listar(
        [FromQuery] ConsultaProveedores consulta,
        CancellationToken cancelacion)
    {
        var resultado = await _proveedores.ListarAsync(consulta, cancelacion);

        return Ok(resultado.Valor);
    }

    /// <summary>Consulta un proveedor.</summary>
    /// <param name="id">Proveedor consultado.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<ProveedorResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProveedorResponse>> Obtener(Guid id, CancellationToken cancelacion)
    {
        var resultado = await _proveedores.ObtenerAsync(id, cancelacion);

        return resultado.EsCorrecto ? Ok(resultado.Valor) : AProblema(resultado.Error!);
    }

    /// <summary>Registra un proveedor.</summary>
    /// <param name="peticion">Datos del proveedor.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpPost]
    [ProducesResponseType<ProveedorResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ProveedorResponse>> Crear(
        CrearProveedorRequest peticion,
        CancellationToken cancelacion)
    {
        var resultado = await _proveedores.CrearAsync(peticion, cancelacion);
        if (!resultado.EsCorrecto)
        {
            return AProblema(resultado.Error!);
        }

        return CreatedAtAction(
            nameof(Obtener),
            new { id = resultado.Valor!.Id, version = "1.0" },
            resultado.Valor);
    }

    /// <summary>Modifica un proveedor.</summary>
    /// <param name="id">Proveedor que se modifica.</param>
    /// <param name="peticion">Nuevos datos.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<ProveedorResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ProveedorResponse>> Actualizar(
        Guid id,
        ActualizarProveedorRequest peticion,
        CancellationToken cancelacion)
    {
        var resultado = await _proveedores.ActualizarAsync(id, peticion, cancelacion);

        return resultado.EsCorrecto ? Ok(resultado.Valor) : AProblema(resultado.Error!);
    }

    /// <summary>Da de baja un proveedor. El borrado es lógico.</summary>
    /// <param name="id">Proveedor que se da de baja.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancelacion)
    {
        var resultado = await _proveedores.EliminarAsync(id, cancelacion);

        return resultado.EsCorrecto ? NoContent() : AProblema(resultado.Error!);
    }

    /// <summary>Consulta las ofertas presentadas por un proveedor.</summary>
    /// <param name="id">Proveedor consultado.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpGet("{id:guid}/ofertas")]
    [ProducesResponseType<IReadOnlyList<OfertaResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<OfertaResponse>>> Ofertas(
        Guid id,
        CancellationToken cancelacion)
    {
        var resultado = await _proveedores.ObtenerOfertasAsync(id, cancelacion);

        return resultado.EsCorrecto ? Ok(resultado.Valor) : AProblema(resultado.Error!);
    }
}
