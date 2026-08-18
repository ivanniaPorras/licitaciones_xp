using Licitaciones.Application.Comun;
using Licitaciones.Application.Licitaciones;
using Licitaciones.Application.Ofertas;
using Licitaciones.Application.Proveedores;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Licitaciones.Web.Controllers;

/// <summary>
/// Pantallas de administración de ofertas. Solo orquesta: las reglas viven en
/// <see cref="IOfertaService"/>.
/// </summary>
[Route("ofertas")]
public sealed class OfertasController : Controller
{
    private readonly IOfertaService _ofertas;
    private readonly ILicitacionService _licitaciones;
    private readonly IProveedorService _proveedores;

    /// <summary>Crea el controlador.</summary>
    /// <param name="ofertas">Casos de uso de ofertas.</param>
    /// <param name="licitaciones">Casos de uso de licitaciones, para poblar los selectores.</param>
    /// <param name="proveedores">Casos de uso de proveedores, para poblar los selectores.</param>
    public OfertasController(
        IOfertaService ofertas,
        ILicitacionService licitaciones,
        IProveedorService proveedores)
    {
        _ofertas = ofertas;
        _licitaciones = licitaciones;
        _proveedores = proveedores;
    }

    /// <summary>Listado con filtro por licitación y por proveedor.</summary>
    /// <param name="consulta">Filtros del listado.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] ConsultaOfertas consulta, CancellationToken cancelacion)
    {
        var resultado = await _ofertas.ListarAsync(consulta, cancelacion);
        await PoblarSelectoresAsync(cancelacion);

        return View(resultado.Valor);
    }

    /// <summary>Detalle de una oferta.</summary>
    /// <param name="id">Oferta consultada.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancelacion)
    {
        var resultado = await _ofertas.ObtenerAsync(id, cancelacion);

        return resultado.EsCorrecto ? View(resultado.Valor) : NotFound();
    }

    /// <summary>Formulario de registro.</summary>
    /// <param name="licitacionId">Licitación preseleccionada, si viene de su detalle.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpGet("crear")]
    public async Task<IActionResult> Create(Guid? licitacionId, CancellationToken cancelacion)
    {
        await PoblarSelectoresAsync(cancelacion);

        return View(new CrearOfertaRequest(licitacionId ?? Guid.Empty, Guid.Empty, 0m));
    }

    /// <summary>Registra la oferta.</summary>
    /// <param name="peticion">Datos del formulario.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpPost("crear")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CrearOfertaRequest peticion, CancellationToken cancelacion)
    {
        var resultado = await _ofertas.CrearAsync(peticion, cancelacion);
        if (!resultado.EsCorrecto)
        {
            AgregarError(resultado.Error!);
            await PoblarSelectoresAsync(cancelacion);
            return View(peticion);
        }

        TempData["Exito"] = "La oferta se registró correctamente.";
        return RedirectToAction(nameof(Details), new { id = resultado.Valor!.Id });
    }

    /// <summary>Formulario de edición.</summary>
    /// <param name="id">Oferta que se edita.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpGet("{id:guid}/editar")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancelacion)
    {
        var resultado = await _ofertas.ObtenerAsync(id, cancelacion);
        if (!resultado.EsCorrecto)
        {
            return NotFound();
        }

        ViewData["Id"] = id;
        ViewData["Oferta"] = resultado.Valor;

        return View(new ActualizarOfertaRequest(resultado.Valor!.MontoOfertadoCRC));
    }

    /// <summary>Guarda el nuevo monto de la oferta.</summary>
    /// <param name="id">Oferta que se edita.</param>
    /// <param name="peticion">Datos del formulario.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpPost("{id:guid}/editar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        Guid id,
        ActualizarOfertaRequest peticion,
        CancellationToken cancelacion)
    {
        var resultado = await _ofertas.ActualizarAsync(id, peticion, cancelacion);
        if (!resultado.EsCorrecto)
        {
            if (resultado.Error!.Tipo == TipoError.NoEncontrado)
            {
                return NotFound();
            }

            var actual = await _ofertas.ObtenerAsync(id, cancelacion);
            ViewData["Id"] = id;
            ViewData["Oferta"] = actual.Valor;
            AgregarError(resultado.Error);
            return View(peticion);
        }

        TempData["Exito"] = "La oferta se actualizó correctamente.";
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>Confirmación previa a la eliminación.</summary>
    /// <param name="id">Oferta que se elimina.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpGet("{id:guid}/eliminar")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancelacion)
    {
        var resultado = await _ofertas.ObtenerAsync(id, cancelacion);

        return resultado.EsCorrecto ? View(resultado.Valor) : NotFound();
    }

    /// <summary>Elimina la oferta.</summary>
    /// <param name="id">Oferta que se elimina.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpPost("{id:guid}/eliminar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id, CancellationToken cancelacion)
    {
        var resultado = await _ofertas.EliminarAsync(id, cancelacion);
        if (!resultado.EsCorrecto)
        {
            TempData["Error"] = resultado.Error!.Mensaje;
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["Exito"] = "La oferta se eliminó correctamente.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PoblarSelectoresAsync(CancellationToken cancelacion)
    {
        var licitaciones = await _licitaciones.ListarAsync(
            new ConsultaLicitaciones { Tamano = ConsultaPaginada.TamanoMaximo },
            cancelacion);
        var proveedores = await _proveedores.ListarAsync(
            new ConsultaProveedores(Tamano: ConsultaPaginada.TamanoMaximo),
            cancelacion);

        ViewData["Licitaciones"] = licitaciones.Valor!.Elementos
            .Select(l => new SelectListItem($"{l.Codigo} — {l.Titulo}", l.Id.ToString()))
            .ToList();
        ViewData["Proveedores"] = proveedores.Valor!.Elementos
            .Select(p => new SelectListItem(p.Nombre, p.Id.ToString()))
            .ToList();
    }

    private void AgregarError(ErrorAplicacion error)
    {
        var campo = error.Codigo switch
        {
            CodigosError.MontoInvalido or CodigosError.OfertaSuperaPresupuesto => "MontoOfertadoCRC",
            CodigosError.OfertaDuplicada or CodigosError.ProveedorNoEncontrado => "ProveedorId",
            CodigosError.LicitacionNoPublicada
                or CodigosError.LicitacionCerrada
                or CodigosError.LicitacionNoEncontrada => "LicitacionId",
            _ => string.Empty
        };

        ModelState.AddModelError(campo, error.Mensaje);
    }
}
