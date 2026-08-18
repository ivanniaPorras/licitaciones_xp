using Licitaciones.Application.Comun;
using Licitaciones.Application.Proveedores;
using Microsoft.AspNetCore.Mvc;

namespace Licitaciones.Web.Controllers;

/// <summary>
/// Pantallas de administración de proveedores. Solo orquesta: toda la lógica vive en
/// <see cref="IProveedorService"/>.
/// </summary>
[Route("proveedores")]
public sealed class ProveedoresController : Controller
{
    private readonly IProveedorService _proveedores;

    /// <summary>Crea el controlador.</summary>
    /// <param name="proveedores">Casos de uso de proveedores.</param>
    public ProveedoresController(IProveedorService proveedores) => _proveedores = proveedores;

    /// <summary>Listado con paginación, filtro y ordenamiento.</summary>
    /// <param name="consulta">Filtros del listado.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpGet("")]
    public async Task<IActionResult> Index(
        [FromQuery] ConsultaProveedores consulta,
        CancellationToken cancelacion)
    {
        var resultado = await _proveedores.ListarAsync(consulta, cancelacion);

        return View(resultado.Valor);
    }

    /// <summary>Detalle de un proveedor.</summary>
    /// <param name="id">Proveedor consultado.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancelacion)
    {
        var resultado = await _proveedores.ObtenerAsync(id, cancelacion);

        return resultado.EsCorrecto ? View(resultado.Valor) : NotFound();
    }

    /// <summary>Formulario de registro.</summary>
    [HttpGet("crear")]
    public IActionResult Create() => View(new CrearProveedorRequest(string.Empty));

    /// <summary>Registra el proveedor.</summary>
    /// <param name="peticion">Datos del formulario.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpPost("crear")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CrearProveedorRequest peticion, CancellationToken cancelacion)
    {
        var resultado = await _proveedores.CrearAsync(peticion, cancelacion);
        if (!resultado.EsCorrecto)
        {
            AgregarErrorAlCampo(resultado.Error!, nameof(CrearProveedorRequest.Nombre));
            return View(peticion);
        }

        TempData["Exito"] = "El proveedor se registró correctamente.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Formulario de edición.</summary>
    /// <param name="id">Proveedor que se edita.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpGet("{id:guid}/editar")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancelacion)
    {
        var resultado = await _proveedores.ObtenerAsync(id, cancelacion);
        if (!resultado.EsCorrecto)
        {
            return NotFound();
        }

        ViewData["Id"] = id;
        return View(new ActualizarProveedorRequest(resultado.Valor!.Nombre));
    }

    /// <summary>Guarda los cambios del proveedor.</summary>
    /// <param name="id">Proveedor que se edita.</param>
    /// <param name="peticion">Datos del formulario.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpPost("{id:guid}/editar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        Guid id,
        ActualizarProveedorRequest peticion,
        CancellationToken cancelacion)
    {
        var resultado = await _proveedores.ActualizarAsync(id, peticion, cancelacion);
        if (!resultado.EsCorrecto)
        {
            if (resultado.Error!.Tipo == TipoError.NoEncontrado)
            {
                return NotFound();
            }

            ViewData["Id"] = id;
            AgregarErrorAlCampo(resultado.Error, nameof(ActualizarProveedorRequest.Nombre));
            return View(peticion);
        }

        TempData["Exito"] = "El proveedor se actualizó correctamente.";
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>Confirmación previa a la baja.</summary>
    /// <param name="id">Proveedor que se da de baja.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpGet("{id:guid}/eliminar")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancelacion)
    {
        var resultado = await _proveedores.ObtenerAsync(id, cancelacion);

        return resultado.EsCorrecto ? View(resultado.Valor) : NotFound();
    }

    /// <summary>Da de baja el proveedor.</summary>
    /// <param name="id">Proveedor que se da de baja.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpPost("{id:guid}/eliminar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id, CancellationToken cancelacion)
    {
        var resultado = await _proveedores.EliminarAsync(id, cancelacion);
        if (!resultado.EsCorrecto)
        {
            TempData["Error"] = resultado.Error!.Mensaje;
            return RedirectToAction(nameof(Index));
        }

        TempData["Exito"] = "El proveedor se dio de baja. Sus ofertas se conservan.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Ofertas presentadas por el proveedor.</summary>
    /// <param name="id">Proveedor consultado.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpGet("{id:guid}/ofertas")]
    public async Task<IActionResult> Ofertas(Guid id, CancellationToken cancelacion)
    {
        var proveedor = await _proveedores.ObtenerAsync(id, cancelacion);
        if (!proveedor.EsCorrecto)
        {
            return NotFound();
        }

        var ofertas = await _proveedores.ObtenerOfertasAsync(id, cancelacion);
        ViewData["Proveedor"] = proveedor.Valor!.Nombre;

        return View(ofertas.Valor);
    }

    // El mensaje se muestra junto al campo que lo provoca, no en un resumen al inicio.
    private void AgregarErrorAlCampo(ErrorAplicacion error, string campo) =>
        ModelState.AddModelError(campo, error.Mensaje);
}
