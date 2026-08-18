using Licitaciones.Application.Comun;
using Licitaciones.Application.Licitaciones;
using Licitaciones.Domain.Licitaciones;
using Microsoft.AspNetCore.Mvc;

namespace Licitaciones.Web.Controllers;

/// <summary>
/// Pantallas de administración de licitaciones. Solo orquesta: la lógica vive en
/// <see cref="ILicitacionService"/>.
/// </summary>
[Route("licitaciones")]
public sealed class LicitacionesController : Controller
{
    private readonly ILicitacionService _licitaciones;

    /// <summary>Crea el controlador.</summary>
    /// <param name="licitaciones">Casos de uso de licitaciones.</param>
    public LicitacionesController(ILicitacionService licitaciones) => _licitaciones = licitaciones;

    /// <summary>Listado con paginación, filtro por estado y ordenamiento.</summary>
    /// <param name="consulta">Filtros del listado.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpGet("")]
    public async Task<IActionResult> Index(
        [FromQuery] ConsultaLicitaciones consulta,
        CancellationToken cancelacion)
    {
        var resultado = await _licitaciones.ListarAsync(consulta, cancelacion);

        return View(resultado.Valor);
    }

    /// <summary>Detalle de una licitación.</summary>
    /// <param name="id">Licitación consultada.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancelacion)
    {
        var resultado = await _licitaciones.ObtenerAsync(id, cancelacion);
        if (!resultado.EsCorrecto)
        {
            return NotFound();
        }

        ViewData["Transiciones"] = MaquinaEstadosLicitacion.TransicionesDesde(resultado.Valor!.Estado);

        return View(resultado.Valor);
    }

    /// <summary>Formulario de registro.</summary>
    [HttpGet("crear")]
    public IActionResult Create() =>
        View(new CrearLicitacionRequest(string.Empty, string.Empty, 0m, DateTimeOffset.UtcNow.AddDays(30)));

    /// <summary>Registra la licitación.</summary>
    /// <param name="peticion">Datos del formulario.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpPost("crear")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CrearLicitacionRequest peticion, CancellationToken cancelacion)
    {
        var resultado = await _licitaciones.CrearAsync(peticion, cancelacion);
        if (!resultado.EsCorrecto)
        {
            AgregarError(resultado.Error!);
            return View(peticion);
        }

        TempData["Exito"] = "La licitación se registró en estado Borrador.";
        return RedirectToAction(nameof(Details), new { id = resultado.Valor!.Id });
    }

    /// <summary>Formulario de edición.</summary>
    /// <param name="id">Licitación que se edita.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpGet("{id:guid}/editar")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancelacion)
    {
        var resultado = await _licitaciones.ObtenerAsync(id, cancelacion);
        if (!resultado.EsCorrecto)
        {
            return NotFound();
        }

        ViewData["Id"] = id;
        var actual = resultado.Valor!;

        return View(new ActualizarLicitacionRequest(
            actual.Codigo,
            actual.Titulo,
            actual.PresupuestoEstimadoCRC,
            actual.FechaCierre));
    }

    /// <summary>Guarda los cambios de la licitación.</summary>
    /// <param name="id">Licitación que se edita.</param>
    /// <param name="peticion">Datos del formulario.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpPost("{id:guid}/editar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        Guid id,
        ActualizarLicitacionRequest peticion,
        CancellationToken cancelacion)
    {
        var resultado = await _licitaciones.ActualizarAsync(id, peticion, cancelacion);
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

        TempData["Exito"] = "La licitación se actualizó correctamente.";
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>Confirmación previa a la baja.</summary>
    /// <param name="id">Licitación que se da de baja.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpGet("{id:guid}/eliminar")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancelacion)
    {
        var resultado = await _licitaciones.ObtenerAsync(id, cancelacion);

        return resultado.EsCorrecto ? View(resultado.Valor) : NotFound();
    }

    /// <summary>Da de baja la licitación.</summary>
    /// <param name="id">Licitación que se da de baja.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpPost("{id:guid}/eliminar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id, CancellationToken cancelacion)
    {
        var resultado = await _licitaciones.EliminarAsync(id, cancelacion);
        if (!resultado.EsCorrecto)
        {
            TempData["Error"] = resultado.Error!.Mensaje;
        }
        else
        {
            TempData["Exito"] = "La licitación se dio de baja. Sus ofertas se conservan.";
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>Aplica una transición de estado, con confirmación previa en la vista.</summary>
    /// <param name="id">Licitación que cambia de estado.</param>
    /// <param name="estado">Estado destino.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpPost("{id:guid}/estado")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(
        Guid id,
        EstadoLicitacion estado,
        CancellationToken cancelacion)
    {
        var resultado = await _licitaciones.CambiarEstadoAsync(
            id,
            new CambiarEstadoRequest(estado),
            cancelacion);

        if (!resultado.EsCorrecto)
        {
            TempData["Error"] = resultado.Error!.Mensaje;
        }
        else
        {
            TempData["Exito"] = $"La licitación pasó a {estado}.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>Ofertas recibidas, con la mejor oferta y su clasificación destacadas.</summary>
    /// <param name="id">Licitación consultada.</param>
    /// <param name="cancelacion">Testigo de cancelación.</param>
    [HttpGet("{id:guid}/ofertas")]
    public async Task<IActionResult> Ofertas(Guid id, CancellationToken cancelacion)
    {
        var licitacion = await _licitaciones.ObtenerAsync(id, cancelacion);
        if (!licitacion.EsCorrecto)
        {
            return NotFound();
        }

        var ofertas = await _licitaciones.ObtenerOfertasAsync(id, cancelacion);
        var mejor = await _licitaciones.ObtenerMejorOfertaAsync(id, cancelacion);

        ViewData["Licitacion"] = licitacion.Valor;
        ViewData["MejorOferta"] = mejor.Valor;

        return View(ofertas.Valor);
    }

    // El mensaje se muestra junto al campo que lo provoca cuando se puede identificar.
    private void AgregarError(ErrorAplicacion error)
    {
        var campo = error.Codigo switch
        {
            CodigosError.CodigoLicitacionDuplicado => "Codigo",
            CodigosError.MontoInvalido or CodigosError.PresupuestoMenorQueOferta => "PresupuestoEstimadoCRC",
            CodigosError.FechaCierreEnElPasado => "FechaCierre",
            _ => string.Empty
        };

        ModelState.AddModelError(campo, error.Mensaje);
    }
}
