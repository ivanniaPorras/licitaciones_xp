using Licitaciones.Application.Comun;
using Licitaciones.Application.Moneda;
using Microsoft.AspNetCore.Mvc;

namespace Licitaciones.Web.Controllers;

/// <summary>
/// Pantallas de administración del tipo de cambio. Solo orquesta: la exclusividad de la
/// tasa activa vive en <see cref="ITipoCambioService"/>.
/// </summary>
[Route("tipos-cambio")]
public sealed class TiposCambioController : Controller
{
    private readonly ITipoCambioService _tiposCambio;

    /// <summary>Crea el controlador.</summary>
    /// <param name="tiposCambio">Casos de uso de tipos de cambio.</param>
    public TiposCambioController(ITipoCambioService tiposCambio) => _tiposCambio = tiposCambio;

    /// <summary>Listado con paginación, búsqueda por año de vigencia y ordenamiento.</summary>
    /// <param name="consulta">Filtros del listado.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpGet("")]
    public async Task<IActionResult> Index(
        [FromQuery] ConsultaTiposCambio consulta,
        CancellationToken cancelacion)
    {
        var resultado = await _tiposCambio.ListarAsync(consulta, cancelacion);

        return View(resultado.Valor);
    }

    /// <summary>Detalle de una tasa.</summary>
    /// <param name="id">Tasa consultada.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancelacion)
    {
        var resultado = await _tiposCambio.ObtenerAsync(id, cancelacion);

        return resultado.EsCorrecto ? View(resultado.Valor) : NotFound();
    }

    /// <summary>Formulario de registro.</summary>
    [HttpGet("crear")]
    public IActionResult Create() =>
        View(new CrearTipoCambioRequest(0m, DateTimeOffset.UtcNow));

    /// <summary>Registra la tasa.</summary>
    /// <param name="peticion">Datos del formulario.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpPost("crear")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CrearTipoCambioRequest peticion,
        CancellationToken cancelacion)
    {
        var resultado = await _tiposCambio.CrearAsync(peticion, cancelacion);
        if (!resultado.EsCorrecto)
        {
            AgregarError(resultado.Error!);
            return View(peticion);
        }

        TempData["Exito"] = "El tipo de cambio se registró correctamente. Actívelo para empezar a usarlo.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Formulario de edición.</summary>
    /// <param name="id">Tasa que se edita.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpGet("{id:guid}/editar")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancelacion)
    {
        var resultado = await _tiposCambio.ObtenerAsync(id, cancelacion);
        if (!resultado.EsCorrecto)
        {
            return NotFound();
        }

        ViewData["Id"] = id;
        var actual = resultado.Valor!;

        return View(new ActualizarTipoCambioRequest(actual.CRCporUSD, actual.FechaVigencia));
    }

    /// <summary>Guarda los cambios de la tasa.</summary>
    /// <param name="id">Tasa que se edita.</param>
    /// <param name="peticion">Datos del formulario.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpPost("{id:guid}/editar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        Guid id,
        ActualizarTipoCambioRequest peticion,
        CancellationToken cancelacion)
    {
        var resultado = await _tiposCambio.ActualizarAsync(id, peticion, cancelacion);
        if (!resultado.EsCorrecto)
        {
            if (resultado.Error!.Tipo == TipoError.NoEncontrado)
            {
                return NotFound();
            }

            ViewData["Id"] = id;
            AgregarError(resultado.Error);
            return View(peticion);
        }

        TempData["Exito"] = "El tipo de cambio se actualizó correctamente.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Pone la tasa en uso y retira de uso a la que lo estuviera.</summary>
    /// <param name="id">Tasa que pasa a estar vigente.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpPost("{id:guid}/activar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activar(Guid id, CancellationToken cancelacion)
    {
        var resultado = await _tiposCambio.ActivarAsync(id, cancelacion);
        if (!resultado.EsCorrecto)
        {
            TempData["Error"] = resultado.Error!.Mensaje;
        }
        else
        {
            TempData["Exito"] = "El tipo de cambio quedó vigente. La tasa anterior dejó de usarse.";
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>Confirmación previa a la eliminación.</summary>
    /// <param name="id">Tasa que se elimina.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpGet("{id:guid}/eliminar")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancelacion)
    {
        var resultado = await _tiposCambio.ObtenerAsync(id, cancelacion);

        return resultado.EsCorrecto ? View(resultado.Valor) : NotFound();
    }

    /// <summary>Elimina la tasa.</summary>
    /// <param name="id">Tasa que se elimina.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpPost("{id:guid}/eliminar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id, CancellationToken cancelacion)
    {
        var resultado = await _tiposCambio.EliminarAsync(id, cancelacion);
        if (!resultado.EsCorrecto)
        {
            TempData["Error"] = resultado.Error!.Mensaje;
        }
        else
        {
            TempData["Exito"] = "El tipo de cambio se eliminó correctamente.";
        }

        return RedirectToAction(nameof(Index));
    }

    private void AgregarError(ErrorAplicacion error) =>
        ModelState.AddModelError(
            error.Codigo == CodigosError.TasaInvalida ? "CRCporUSD" : string.Empty,
            error.Mensaje);
}
