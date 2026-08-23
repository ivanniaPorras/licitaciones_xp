using Licitaciones.Application.Aprobacion;
using Licitaciones.Application.Comun;
using Microsoft.AspNetCore.Mvc;

namespace Licitaciones.Web.Controllers;

/// <summary>
/// Pantallas de administración de los niveles de aprobación. Solo orquesta: las reglas de
/// traslape viven en <see cref="INivelAprobacionService"/>.
/// </summary>
[Route("niveles-aprobacion")]
public sealed class NivelesAprobacionController : Controller
{
    private readonly INivelAprobacionService _niveles;

    /// <summary>Crea el controlador.</summary>
    /// <param name="niveles">Casos de uso de niveles de aprobación.</param>
    public NivelesAprobacionController(INivelAprobacionService niveles) => _niveles = niveles;

    /// <summary>Listado con paginación, búsqueda por aprobador y ordenamiento.</summary>
    /// <param name="consulta">Filtros del listado.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpGet("")]
    public async Task<IActionResult> Index(
        [FromQuery] ConsultaNivelesAprobacion consulta,
        CancellationToken cancelacion)
    {
        var resultado = await _niveles.ListarAsync(consulta, cancelacion);

        return View(resultado.Valor);
    }

    /// <summary>Detalle de un nivel.</summary>
    /// <param name="id">Nivel consultado.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancelacion)
    {
        var resultado = await _niveles.ObtenerAsync(id, cancelacion);

        return resultado.EsCorrecto ? View(resultado.Valor) : NotFound();
    }

    /// <summary>Formulario de registro.</summary>
    [HttpGet("crear")]
    public IActionResult Create() => View(new CrearNivelAprobacionRequest(0m, null, string.Empty));

    /// <summary>Registra el nivel.</summary>
    /// <param name="peticion">Datos del formulario.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpPost("crear")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CrearNivelAprobacionRequest peticion,
        CancellationToken cancelacion)
    {
        var resultado = await _niveles.CrearAsync(peticion, cancelacion);
        if (!resultado.EsCorrecto)
        {
            AgregarError(resultado.Error!);
            return View(peticion);
        }

        TempData["Exito"] = "El nivel de aprobación se registró correctamente.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Formulario de edición.</summary>
    /// <param name="id">Nivel que se edita.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpGet("{id:guid}/editar")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancelacion)
    {
        var resultado = await _niveles.ObtenerAsync(id, cancelacion);
        if (!resultado.EsCorrecto)
        {
            return NotFound();
        }

        ViewData["Id"] = id;
        var actual = resultado.Valor!;

        return View(new ActualizarNivelAprobacionRequest(
            actual.MontoMinimoCRC,
            actual.MontoMaximoCRC,
            actual.Aprobador));
    }

    /// <summary>Guarda los cambios del nivel.</summary>
    /// <param name="id">Nivel que se edita.</param>
    /// <param name="peticion">Datos del formulario.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpPost("{id:guid}/editar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        Guid id,
        ActualizarNivelAprobacionRequest peticion,
        CancellationToken cancelacion)
    {
        var resultado = await _niveles.ActualizarAsync(id, peticion, cancelacion);
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

        TempData["Exito"] = "El nivel de aprobación se actualizó correctamente.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Confirmación previa a la eliminación.</summary>
    /// <param name="id">Nivel que se elimina.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpGet("{id:guid}/eliminar")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancelacion)
    {
        var resultado = await _niveles.ObtenerAsync(id, cancelacion);

        return resultado.EsCorrecto ? View(resultado.Valor) : NotFound();
    }

    /// <summary>Elimina el nivel.</summary>
    /// <param name="id">Nivel que se elimina.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpPost("{id:guid}/eliminar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id, CancellationToken cancelacion)
    {
        var resultado = await _niveles.EliminarAsync(id, cancelacion);
        if (!resultado.EsCorrecto)
        {
            TempData["Error"] = resultado.Error!.Mensaje;
        }
        else
        {
            TempData["Exito"] = "El nivel de aprobación se eliminó correctamente.";
        }

        return RedirectToAction(nameof(Index));
    }

    private void AgregarError(ErrorAplicacion error)
    {
        var campo = error.Codigo switch
        {
            CodigosError.RangoTraslapado or CodigosError.MontoInvalido => "MontoMinimoCRC",
            CodigosError.RangoAbiertoDuplicado or CodigosError.RangoInvalido => "MontoMaximoCRC",
            _ => string.Empty
        };

        ModelState.AddModelError(campo, error.Mensaje);
    }
}
