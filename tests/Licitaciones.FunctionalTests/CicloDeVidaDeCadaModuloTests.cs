using Licitaciones.FunctionalTests.Apoyo;
using Microsoft.Playwright;

namespace Licitaciones.FunctionalTests;

/// <summary>
/// Recorre en el navegador la creación, consulta, edición y eliminación de los cinco
/// módulos, y comprueba que los mensajes de validación aparecen junto a su campo
/// (HU-035, criterios 2 y 3).
/// </summary>
[Collection(NavegadorCollection.Nombre)]
public sealed class CicloDeVidaDeCadaModuloTests
{
    private readonly AplicacionEnNavegador _aplicacion;

    public CicloDeVidaDeCadaModuloTests(AplicacionEnNavegador aplicacion) => _aplicacion = aplicacion;

    [Fact]
    public async Task Proveedores_RecorridoCompleto()
    {
        var pagina = await _aplicacion.AbrirPaginaAsync();
        var sufijo = Sufijo();
        var nombre = $"Proveedor {sufijo}";

        // Crear
        await pagina.GotoAsync("/proveedores/crear");
        await pagina.FillAsync("#Nombre", nombre);
        await Guardar(pagina);
        await Assertions.Expect(pagina.GetByText("El proveedor se registró correctamente."))
            .ToBeVisibleAsync();

        // Consultar
        await pagina.GotoAsync($"/proveedores?busqueda={sufijo}");
        await Assertions.Expect(pagina.GetByText(nombre)).ToBeVisibleAsync();
        await pagina.GetByRole(AriaRole.Link, new() { Name = "Ver" }).First.ClickAsync();
        await Assertions.Expect(pagina.GetByRole(AriaRole.Heading, new() { Name = nombre }))
            .ToBeVisibleAsync();

        // Editar
        var renombrado = $"Proveedor editado {sufijo}";
        await pagina.GetByRole(AriaRole.Link, new() { Name = "Editar" }).First.ClickAsync();
        await pagina.FillAsync("#Nombre", renombrado);
        await Guardar(pagina, "Guardar cambios");
        await Assertions.Expect(pagina.GetByText("El proveedor se actualizó correctamente."))
            .ToBeVisibleAsync();

        // Eliminar, con confirmación previa
        await pagina.GotoAsync($"/proveedores?busqueda={sufijo}");
        await pagina.GetByRole(AriaRole.Link, new() { Name = "Eliminar" }).First.ClickAsync();
        await Assertions.Expect(pagina.GetByText("¿Confirma", new() { Exact = false })).ToBeVisibleAsync();
        await pagina.GetByRole(AriaRole.Button, new() { Name = "Sí, dar de baja" }).ClickAsync();

        await pagina.GotoAsync($"/proveedores?busqueda={sufijo}");
        await Assertions.Expect(pagina.GetByText(renombrado)).ToBeHiddenAsync();
    }

    [Fact]
    public async Task NivelesAprobacion_RecorridoCompleto()
    {
        var pagina = await _aplicacion.AbrirPaginaAsync();
        var aprobador = $"Comite {Sufijo()}";

        // El último nivel de la semilla no tiene monto máximo, así que cubre todo lo que
        // esté por encima de diez millones y cualquier rango nuevo se traslaparía con él.
        // Para abrir sitio se le pone techo, y al final se le devuelve.
        await CambiarTechoDeJuntaDirectivaAsync(pagina, "99999999.99");

        // Crear el rango que queda libre por arriba.
        await pagina.GotoAsync("/niveles-aprobacion/crear");
        await pagina.FillAsync("#MontoMinimoCRC", "100000000.00");
        await pagina.FillAsync("#Aprobador", aprobador);
        await Guardar(pagina);
        await Assertions.Expect(pagina.GetByText("El nivel de aprobación se registró correctamente."))
            .ToBeVisibleAsync();

        // Consultar
        await pagina.GotoAsync($"/niveles-aprobacion?busqueda={aprobador}");
        await Assertions.Expect(pagina.GetByText(aprobador).First).ToBeVisibleAsync();

        // Editar, poniéndole un techo al rango recién creado.
        await pagina.GetByRole(AriaRole.Link, new() { Name = "Editar" }).First.ClickAsync();
        await pagina.FillAsync("#MontoMaximoCRC", "500000000.00");
        await Guardar(pagina, "Guardar cambios");
        await Assertions.Expect(pagina.GetByText("El nivel de aprobación se actualizó correctamente."))
            .ToBeVisibleAsync();

        // Eliminar
        await pagina.GotoAsync($"/niveles-aprobacion?busqueda={aprobador}");
        await pagina.GetByRole(AriaRole.Link, new() { Name = "Eliminar" }).First.ClickAsync();
        await pagina.GetByRole(AriaRole.Button, new() { Name = "Sí, eliminar" }).ClickAsync();
        await Assertions.Expect(pagina.GetByText("El nivel de aprobación se eliminó correctamente."))
            .ToBeVisibleAsync();

        // Se devuelve la semilla a su estado original para no alterar a las demás pruebas.
        await CambiarTechoDeJuntaDirectivaAsync(pagina, string.Empty);
    }

    /// <summary>Cambia el monto máximo del nivel de Junta Directiva, o se lo quita.</summary>
    /// <param name="pagina">Pestaña en uso.</param>
    /// <param name="montoMaximo">Nuevo techo, o cadena vacía para dejar el rango abierto.</param>
    private static async Task CambiarTechoDeJuntaDirectivaAsync(IPage pagina, string montoMaximo)
    {
        await pagina.GotoAsync("/niveles-aprobacion?busqueda=Junta");
        await pagina.GetByRole(AriaRole.Link, new() { Name = "Editar" }).First.ClickAsync();
        await pagina.FillAsync("#MontoMaximoCRC", montoMaximo);
        await Guardar(pagina, "Guardar cambios");
        await Assertions.Expect(pagina.GetByText("El nivel de aprobación se actualizó correctamente."))
            .ToBeVisibleAsync();
    }

    [Fact]
    public async Task TiposCambio_RecorridoCompletoYActivacion()
    {
        var pagina = await _aplicacion.AbrirPaginaAsync();
        pagina.Dialog += async (_, dialogo) => await dialogo.AcceptAsync();

        // Crear. Nace fuera de uso.
        await pagina.GotoAsync("/tipos-cambio/crear");
        await pagina.FillAsync("#CRCporUSD", "545.2500");
        await pagina.FillAsync("#FechaVigencia", "2026-11-15");
        await Guardar(pagina);
        await Assertions.Expect(pagina.GetByText("El tipo de cambio se registró correctamente.", new() { Exact = false }))
            .ToBeVisibleAsync();

        await pagina.GotoAsync("/tipos-cambio?busqueda=2026&orden=tasa:desc");
        await Assertions.Expect(pagina.GetByText("545,2500")).ToBeVisibleAsync();
        await Assertions.Expect(pagina.GetByText("Fuera de uso").First).ToBeVisibleAsync();

        // Activar. La tasa anterior deja de estarlo.
        await pagina.GetByRole(AriaRole.Button, new() { Name = "Activar" }).First.ClickAsync();
        await Assertions.Expect(pagina.GetByText("El tipo de cambio quedó vigente.", new() { Exact = false }))
            .ToBeVisibleAsync();

        // Nunca hay más de una tasa en uso.
        await pagina.GotoAsync("/tipos-cambio?tamano=100");
        Assert.Equal(1, await pagina.GetByText("En uso").CountAsync());

        // Editar
        await pagina.GotoAsync("/tipos-cambio?busqueda=2026&orden=tasa:desc");
        await pagina.GetByRole(AriaRole.Link, new() { Name = "Editar" }).First.ClickAsync();
        await pagina.FillAsync("#CRCporUSD", "546.0000");
        await Guardar(pagina, "Guardar cambios");
        await Assertions.Expect(pagina.GetByText("El tipo de cambio se actualizó correctamente."))
            .ToBeVisibleAsync();

        // Antes de borrarla se devuelve el uso a la tasa de la semilla. Si no, el sistema
        // quedaría sin tasa vigente y el resto de las pantallas no podría leer en dólares.
        await pagina.GotoAsync("/tipos-cambio?busqueda=2026&orden=tasa:asc");
        await pagina.GetByRole(AriaRole.Button, new() { Name = "Activar" }).First.ClickAsync();
        await Assertions.Expect(pagina.GetByText("El tipo de cambio quedó vigente.", new() { Exact = false }))
            .ToBeVisibleAsync();

        // Eliminar
        await pagina.GotoAsync("/tipos-cambio?busqueda=2026&orden=tasa:desc");
        await pagina.GetByRole(AriaRole.Link, new() { Name = "Eliminar" }).First.ClickAsync();
        await pagina.GetByRole(AriaRole.Button, new() { Name = "Sí, eliminar" }).ClickAsync();
        await Assertions.Expect(pagina.GetByText("El tipo de cambio se eliminó correctamente."))
            .ToBeVisibleAsync();

        // Queda exactamente una tasa en uso, la de la semilla.
        await pagina.GotoAsync("/tipos-cambio?tamano=100");
        Assert.Equal(1, await pagina.GetByText("En uso").CountAsync());
        await Assertions.Expect(pagina.GetByText("512,0000")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task LicitacionesYOfertas_RecorridoCompleto()
    {
        var pagina = await _aplicacion.AbrirPaginaAsync();
        pagina.Dialog += async (_, dialogo) => await dialogo.AcceptAsync();
        var sufijo = Sufijo();
        var codigo = $"CICLO-{sufijo}";

        // Crear
        await pagina.GotoAsync("/licitaciones/crear");
        await pagina.FillAsync("#Codigo", codigo);
        await pagina.FillAsync("#Titulo", "Servicio de mantenimiento");
        await pagina.FillAsync("#PresupuestoEstimadoCRC", "4000000.00");
        await pagina.FillAsync("#FechaCierre", "2027-12-01T12:00");
        await Guardar(pagina);
        await Assertions.Expect(pagina.GetByText("La licitación se registró en estado Borrador."))
            .ToBeVisibleAsync();

        // Editar
        await pagina.GetByRole(AriaRole.Link, new() { Name = "Editar" }).First.ClickAsync();
        await pagina.FillAsync("#Titulo", "Servicio de mantenimiento anual");
        await Guardar(pagina, "Guardar cambios");
        await Assertions.Expect(pagina.GetByText("La licitación se actualizó correctamente."))
            .ToBeVisibleAsync();

        // Publicar, ofertar y comprobar el detalle de la oferta
        await pagina.GotoAsync($"/licitaciones?busqueda={codigo}");
        await pagina.GetByRole(AriaRole.Link, new() { Name = "Ver" }).First.ClickAsync();
        await pagina.GetByRole(AriaRole.Button, new() { Name = "Pasar a Publicada" }).ClickAsync();

        var proveedor = $"Mantenimientos {sufijo}";
        await pagina.GotoAsync("/proveedores/crear");
        await pagina.FillAsync("#Nombre", proveedor);
        await Guardar(pagina);

        await pagina.GotoAsync("/ofertas/crear");
        await SeleccionarPorTextoAsync(pagina, "#LicitacionId", codigo);
        await SeleccionarPorTextoAsync(pagina, "#ProveedorId", proveedor);
        await pagina.FillAsync("#MontoOfertadoCRC", "3500000.00");
        await Guardar(pagina);
        await Assertions.Expect(pagina.GetByText("La oferta se registró correctamente.")).ToBeVisibleAsync();

        await pagina.GotoAsync($"/ofertas?busqueda={codigo}");
        await pagina.GetByRole(AriaRole.Link, new() { Name = "Ver" }).First.ClickAsync();
        await Assertions.Expect(pagina.GetByText(proveedor).First).ToBeVisibleAsync();

        // Eliminar la oferta y después la licitación
        await pagina.GotoAsync($"/ofertas?busqueda={codigo}");
        await pagina.GetByRole(AriaRole.Link, new() { Name = "Ver" }).First.ClickAsync();
        await pagina.GetByRole(AriaRole.Link, new() { Name = "Eliminar" }).First.ClickAsync();
        await pagina.GetByRole(AriaRole.Button, new() { Name = "Sí, eliminar" }).ClickAsync();
        await Assertions.Expect(pagina.GetByText("La oferta se eliminó correctamente.")).ToBeVisibleAsync();

        await pagina.GotoAsync($"/licitaciones?busqueda={codigo}");
        await pagina.GetByRole(AriaRole.Link, new() { Name = "Ver" }).First.ClickAsync();
        await pagina.GetByRole(AriaRole.Link, new() { Name = "Eliminar" }).First.ClickAsync();
        await pagina.GetByRole(AriaRole.Button, new() { Name = "Sí, dar de baja" }).ClickAsync();
        await Assertions.Expect(pagina.GetByText("La licitación se dio de baja.", new() { Exact = false }))
            .ToBeVisibleAsync();
    }

    [Fact]
    public async Task LosMensajesDeValidacionAparecenJuntoASuCampo()
    {
        var pagina = await _aplicacion.AbrirPaginaAsync();

        // Un nombre con un carácter no admitido. La comprobación del navegador se salta a
        // propósito, para llegar a la del servidor, que es la que debe responder.
        await pagina.GotoAsync("/proveedores/crear");
        await pagina.EvaluateAsync("() => document.querySelector('#Nombre').removeAttribute('pattern')");
        await pagina.FillAsync("#Nombre", "Empresa@Central");
        await Guardar(pagina);

        var mensaje = pagina.Locator("span[data-valmsg-for='Nombre'], #Nombre ~ span.text-danger").First;
        await Assertions.Expect(mensaje).ToContainTextAsync("El nombre solo admite letras");

        // El mensaje está dentro del mismo bloque que el campo, no en un aviso suelto
        // arriba de la página.
        var estanJuntos = await pagina.EvaluateAsync<bool>(
            @"() => {
                const campo = document.querySelector('#Nombre');
                const aviso = document.querySelector('.text-danger.field-validation-error');
                return aviso !== null && campo.closest('.mb-3').contains(aviso);
            }");

        Assert.True(estanJuntos, "El mensaje de validación no acompaña al campo que lo provoca.");
    }

    [Fact]
    public async Task UnRangoDeAprobacionQueSeTraslapa_MuestraElMensajeAcordado()
    {
        var pagina = await _aplicacion.AbrirPaginaAsync();

        // 500 000 cae dentro del primer rango de la semilla.
        await pagina.GotoAsync("/niveles-aprobacion/crear");
        await pagina.FillAsync("#MontoMinimoCRC", "500000.00");
        await pagina.FillAsync("#MontoMaximoCRC", "600000.00");
        await pagina.FillAsync("#Aprobador", "Intruso");
        await Guardar(pagina);

        await Assertions.Expect(pagina.GetByText("El rango se traslapa con un nivel existente."))
            .ToBeVisibleAsync();
    }

    private static string Sufijo() => Guid.NewGuid().ToString("N")[..6];

    private static Task Guardar(IPage pagina, string texto = "Guardar") =>
        pagina.GetByRole(AriaRole.Button, new() { Name = texto }).ClickAsync();

    private static async Task SeleccionarPorTextoAsync(IPage pagina, string selector, string fragmento)
    {
        var valor = await pagina
            .Locator($"{selector} option")
            .Filter(new LocatorFilterOptions { HasTextString = fragmento })
            .First
            .GetAttributeAsync("value");

        await pagina.Locator(selector).SelectOptionAsync(valor!);
    }
}
