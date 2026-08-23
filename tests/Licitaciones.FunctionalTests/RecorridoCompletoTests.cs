using Licitaciones.FunctionalTests.Apoyo;
using Microsoft.Playwright;

namespace Licitaciones.FunctionalTests;

/// <summary>
/// Recorre en un navegador real el flujo completo del sistema: página inicial, cambio de
/// tema, registro de proveedor, creación y publicación de licitación, oferta válida, los
/// tres rechazos, mejor oferta con su clasificación y su aprobador, y alternancia entre
/// colones y dólares (HU-035).
/// </summary>
[Collection(NavegadorCollection.Nombre)]
public sealed class RecorridoCompletoTests
{
    private readonly AplicacionEnNavegador _aplicacion;

    public RecorridoCompletoTests(AplicacionEnNavegador aplicacion) => _aplicacion = aplicacion;

    [Fact]
    public async Task ElFlujoCompletoFuncionaDesdeElNavegador()
    {
        var pagina = await _aplicacion.AbrirPaginaAsync();
        var sufijo = Guid.NewGuid().ToString("N")[..6];

        // Los recursos que no cargan se apuntan aparte: sin ellos la página se vería sin
        // estilos ni guiones, y el fallo aparecería mucho más adelante y sin explicación.
        var recursosFallidos = new List<string>();
        pagina.Response += (_, respuesta) =>
        {
            if (respuesta.Status >= 400)
            {
                recursosFallidos.Add($"{respuesta.Status} {respuesta.Url}");
            }
        };

        // Un guion que falla en silencio dejaría la página funcionando a medias sin que
        // ninguna aserción explicara por qué.
        var erroresDeGuion = new List<string>();
        pagina.PageError += (_, error) => erroresDeGuion.Add(error);

        // ---- Página inicial ----
        await pagina.GotoAsync("/");
        Assert.True(
            await pagina.EvaluateAsync<bool>("() => typeof window.TemaLicitaciones !== 'undefined'"),
            $"El guion del tema no se cargó. Recursos fallidos: {string.Join(", ", recursosFallidos)}");
        await Assertions.Expect(pagina.GetByRole(AriaRole.Heading, new() { Name = "Sistema de Gestión de Licitaciones" }))
            .ToBeVisibleAsync();
        await Assertions.Expect(pagina.Locator("img.diagrama-flujo")).ToBeVisibleAsync();
        await Assertions.Expect(pagina.GetByText("Borrador", new() { Exact = false }).First).ToBeVisibleAsync();

        // ---- Cambio de tema ----
        await pagina.Locator("#alternador-tema button[data-tema='dark']").ClickAsync();
        await Assertions.Expect(pagina.Locator("html")).ToHaveAttributeAsync("data-bs-theme", "dark");

        // La preferencia sobrevive a recargar y a navegar.
        await pagina.ReloadAsync();
        await Assertions.Expect(pagina.Locator("html")).ToHaveAttributeAsync("data-bs-theme", "dark");

        // ---- Registro de proveedor ----
        var proveedor = $"Constructora {sufijo}";
        await pagina.GotoAsync("/proveedores/crear");
        await pagina.FillAsync("#Nombre", proveedor);
        await pagina.GetByRole(AriaRole.Button, new() { Name = "Guardar" }).ClickAsync();
        await EsperarMensajeAsync(pagina, "El proveedor se registró correctamente.");

        // Un nombre equivalente se rechaza con el mensaje acordado.
        await pagina.GotoAsync("/proveedores/crear");
        await pagina.FillAsync("#Nombre", $"  constructora   {sufijo}  ");
        await pagina.GetByRole(AriaRole.Button, new() { Name = "Guardar" }).ClickAsync();
        await Assertions.Expect(pagina.GetByText("Ya existe un proveedor con ese nombre.")).ToBeVisibleAsync();

        // ---- Creación y publicación de licitación ----
        var codigo = $"LIC-{sufijo}";
        await pagina.GotoAsync("/licitaciones/crear");
        await pagina.FillAsync("#Codigo", codigo);
        await pagina.FillAsync("#Titulo", "Compra de equipo de cómputo");
        await pagina.FillAsync("#PresupuestoEstimadoCRC", "10000000.00");
        await pagina.FillAsync("#FechaCierre", "2027-06-30T17:00");
        await pagina.GetByRole(AriaRole.Button, new() { Name = "Guardar" }).ClickAsync();
        await EsperarMensajeAsync(pagina, "La licitación se registró en estado Borrador.");

        await AbrirDetalleDeLicitacionAsync(pagina, codigo);
        await Assertions.Expect(pagina.GetByText("Borrador").First).ToBeVisibleAsync();

        pagina.Dialog += async (_, dialogo) => await dialogo.AcceptAsync();
        await pagina.GetByRole(AriaRole.Button, new() { Name = "Pasar a Publicada" }).ClickAsync();
        await Assertions.Expect(pagina.GetByText("Publicada").First).ToBeVisibleAsync();

        // ---- Oferta válida ----
        await RegistrarOfertaAsync(pagina, codigo, proveedor, "8000000.00");
        await EsperarMensajeAsync(pagina, "La oferta se registró correctamente.");

        // ---- Rechazo por oferta duplicada ----
        await RegistrarOfertaAsync(pagina, codigo, proveedor, "7000000.00");
        await Assertions.Expect(
            pagina.GetByText("Este proveedor ya registró una oferta para esta licitación."))
            .ToBeVisibleAsync();

        // ---- Rechazo por superar el presupuesto ----
        var otroProveedor = $"Suministros {sufijo}";
        await CrearProveedorAsync(pagina, otroProveedor);
        await RegistrarOfertaAsync(pagina, codigo, otroProveedor, "10000000.01");
        await Assertions.Expect(
            pagina.GetByText("La oferta no puede superar el presupuesto de la licitación."))
            .ToBeVisibleAsync();

        // ---- Rechazo sobre una licitación que no está publicada ----
        var codigoBorrador = $"LIC-B{sufijo}";
        await pagina.GotoAsync("/licitaciones/crear");
        await pagina.FillAsync("#Codigo", codigoBorrador);
        await pagina.FillAsync("#Titulo", "Proceso todavía en preparación");
        await pagina.FillAsync("#PresupuestoEstimadoCRC", "5000000.00");
        await pagina.FillAsync("#FechaCierre", "2027-08-31T17:00");
        await pagina.GetByRole(AriaRole.Button, new() { Name = "Guardar" }).ClickAsync();

        await RegistrarOfertaAsync(pagina, codigoBorrador, otroProveedor, "1000000.00");
        await Assertions.Expect(pagina.GetByText("La licitación no está publicada.")).ToBeVisibleAsync();

        // ---- Mejor oferta con su clasificación y su aprobador ----
        await AbrirDetalleDeLicitacionAsync(pagina, codigo);
        await pagina.GetByRole(AriaRole.Link, new() { Name = "Ver ofertas y mejor oferta" }).ClickAsync();

        // 8 000 000 sobre 10 000 000 es un 20 % de ahorro, y el monto cae en el rango de
        // Gerencia según la tabla sembrada.
        await Assertions.Expect(pagina.GetByText("Oferta conveniente").First).ToBeVisibleAsync();
        await Assertions.Expect(pagina.GetByText("Gerencia").First).ToBeVisibleAsync();

        // ---- Alternancia entre colones y dólares ----
        var monto = pagina.Locator("[data-crc]").First;
        await Assertions.Expect(monto).ToContainTextAsync("₡");

        await pagina.Locator("#alternador-moneda button[data-moneda='USD']").ClickAsync();
        Assert.Empty(erroresDeGuion);
        Assert.Contains(
            "$",
            await monto.InnerTextAsync(),
            StringComparison.Ordinal);

        // Junto al monto convertido se muestra siempre la tasa usada y su fecha.
        await Assertions.Expect(pagina.Locator("#alternador-moneda-tasa"))
            .ToContainTextAsync("vigente desde");

        // Volver a colones restituye el valor tal como está almacenado.
        await pagina.Locator("#alternador-moneda button[data-moneda='CRC']").ClickAsync();
        await Assertions.Expect(monto).ToContainTextAsync("₡");
    }

    /// <summary>
    /// Espera un mensaje concreto y, si no aparece, falla contando qué decía la página.
    /// </summary>
    /// <remarks>
    /// Sin esto, el fallo diría solo que el texto no estaba visible, y habría que repetir
    /// el recorrido a mano para averiguar qué mostró realmente el formulario.
    /// </remarks>
    private static async Task EsperarMensajeAsync(IPage pagina, string mensaje)
    {
        try
        {
            await Assertions.Expect(pagina.GetByText(mensaje)).ToBeVisibleAsync();
        }
        catch (PlaywrightException)
        {
            var texto = await pagina.EvaluateAsync<string>("() => document.body.innerText");
            var camposInvalidos = await pagina.EvaluateAsync<string>(
                @"() => Array.from(document.querySelectorAll('input, select'))
                        .filter(c => !c.checkValidity())
                        .map(c => c.id + '=[' + c.value + '] ' + c.validationMessage)
                        .join(' | ')");

            throw new PlaywrightException(
                $"No apareció el mensaje \"{mensaje}\" en {pagina.Url}."
                + $" Campos inválidos: {camposInvalidos}."
                + $" La página decía: {texto}");
        }
    }

    private static async Task CrearProveedorAsync(IPage pagina, string nombre)
    {
        await pagina.GotoAsync("/proveedores/crear");
        await pagina.FillAsync("#Nombre", nombre);
        await pagina.GetByRole(AriaRole.Button, new() { Name = "Guardar" }).ClickAsync();
    }

    private static async Task AbrirDetalleDeLicitacionAsync(IPage pagina, string codigo)
    {
        await pagina.GotoAsync($"/licitaciones?busqueda={codigo}");
        await pagina.GetByRole(AriaRole.Link, new() { Name = "Ver" }).First.ClickAsync();
    }

    private static async Task RegistrarOfertaAsync(
        IPage pagina,
        string codigoLicitacion,
        string nombreProveedor,
        string monto)
    {
        await pagina.GotoAsync("/ofertas/crear");
        await SeleccionarPorTextoAsync(pagina, "#LicitacionId", codigoLicitacion);
        await SeleccionarPorTextoAsync(pagina, "#ProveedorId", nombreProveedor);
        await pagina.FillAsync("#MontoOfertadoCRC", monto);
        await pagina.GetByRole(AriaRole.Button, new() { Name = "Guardar" }).ClickAsync();
    }

    /// <summary>
    /// Elige la opción de un desplegable por un fragmento de su texto. Las etiquetas de
    /// licitación llevan el código y el título juntos, así que no sirve una coincidencia
    /// exacta.
    /// </summary>
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
