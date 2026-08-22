using System.Net;
using System.Net.Http.Json;
using System.Text;
using Licitaciones.Application.Aprobacion;
using Licitaciones.Application.Comun;
using Licitaciones.Application.Moneda;
using Licitaciones.Application.Proveedores;
using Licitaciones.IntegrationTests.Apoyo;

namespace Licitaciones.IntegrationTests.Api;

/// <summary>
/// Verifica el contrato transversal de la API contra la aplicación real: versionado,
/// paginación con su total, códigos de estado y cuerpos de error seguros
/// (HU-030, HU-031, HU-032).
/// </summary>
[Collection(PostgresCollection.Nombre)]
public sealed class ContratoApiTests : IDisposable
{
    private readonly ApiFactory _api;
    private readonly HttpClient _cliente;

    public ContratoApiTests(PostgresFixture postgres)
    {
        ArgumentNullException.ThrowIfNull(postgres);

        _api = new ApiFactory(postgres.CadenaConexion);
        _cliente = _api.CreateClient();
    }

    public void Dispose()
    {
        _cliente.Dispose();
        _api.Dispose();
    }

    // ---- HU-031 · Versionado y forma de los listados ----

    [Theory]
    [InlineData("proveedores")]
    [InlineData("licitaciones")]
    [InlineData("ofertas")]
    [InlineData("niveles-aprobacion")]
    [InlineData("tipos-cambio")]
    public async Task Listados_RespondenBajoLaVersionUnoConSuTotal(string recurso)
    {
        var pagina = await _cliente.GetFromJsonAsync<PaginaDePrueba>(
            $"/api/v1/{recurso}?pagina=1&tamano=2");

        Assert.NotNull(pagina);
        Assert.Equal(1, pagina.Pagina);
        Assert.Equal(2, pagina.Tamano);
        Assert.True(pagina.Total >= 0);
        Assert.True(pagina.Elementos.Count <= 2);
    }

    [Fact]
    public async Task Listado_ConTamanoSuperiorAlMaximo_LoAcotaEnLugarDeDevolverTodo()
    {
        var pagina = await _cliente.GetFromJsonAsync<PaginaDePrueba>(
            "/api/v1/proveedores?tamano=5000");

        // Una petición no puede pedir la tabla entera.
        Assert.Equal(ConsultaPaginada.TamanoMaximo, pagina!.Tamano);
    }

    [Fact]
    public async Task Listado_ConPaginaCero_SeCorrigeALaPrimera()
    {
        var pagina = await _cliente.GetFromJsonAsync<PaginaDePrueba>(
            "/api/v1/proveedores?pagina=0");

        Assert.Equal(1, pagina!.Pagina);
    }

    [Fact]
    public async Task Listado_DeNiveles_AceptaBusquedaYOrden()
    {
        var pagina = await _cliente.GetFromJsonAsync<PagedResponse<NivelAprobacionResponse>>(
            "/api/v1/niveles-aprobacion?busqueda=gerencia&orden=aprobador:asc");

        Assert.All(
            pagina!.Elementos,
            nivel => Assert.Contains("erencia", nivel.Aprobador, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Listado_DeTiposCambio_FiltraPorAnioDeVigencia()
    {
        var pagina = await _cliente.GetFromJsonAsync<PagedResponse<TipoCambioResponse>>(
            "/api/v1/tipos-cambio?busqueda=2026");

        Assert.NotEmpty(pagina!.Elementos);
        Assert.All(
            pagina.Elementos,
            tasa => Assert.Equal(2026, tasa.FechaVigencia.UtcDateTime.Year));
    }

    [Fact]
    public async Task Listado_DeTiposCambio_ConAnioSinTasas_DevuelveLaPaginaVacia()
    {
        var pagina = await _cliente.GetFromJsonAsync<PagedResponse<TipoCambioResponse>>(
            "/api/v1/tipos-cambio?busqueda=1999");

        Assert.Empty(pagina!.Elementos);
        Assert.Equal(0, pagina.Total);
    }

    // ---- HU-031 · Códigos de estado de cada operación ----

    [Fact]
    public async Task CicloCompletoDeUnRecurso_DevuelveLosCodigosAcordados()
    {
        var creacion = await _cliente.PostAsJsonAsync(
            "/api/v1/proveedores",
            new CrearProveedorRequest($"Contrato {Guid.NewGuid():N}"[..24]));

        Assert.Equal(HttpStatusCode.Created, creacion.StatusCode);
        Assert.NotNull(creacion.Headers.Location);

        var creado = (await creacion.Content.ReadFromJsonAsync<ProveedorResponse>())!;

        var consulta = await _cliente.GetAsync(
            new Uri($"/api/v1/proveedores/{creado.Id}", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, consulta.StatusCode);

        var eliminacion = await _cliente.DeleteAsync(
            new Uri($"/api/v1/proveedores/{creado.Id}", UriKind.Relative));
        Assert.Equal(HttpStatusCode.NoContent, eliminacion.StatusCode);
    }

    // ---- HU-032 · Errores comprensibles y seguros ----

    [Fact]
    public async Task RutaInexistente_DevuelveNoEncontradoConCuerpoCompleto()
    {
        var respuesta = await _cliente.GetAsync(new Uri("/api/v1/inventado", UriKind.Relative));

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);

        var problema = await respuesta.Content.ReadFromJsonAsync<ProblemaDetallado>();
        Assert.Equal(CodigosError.RutaNoEncontrada, problema!.Code);
        Assert.False(string.IsNullOrWhiteSpace(problema.CorrelationId));
        Assert.False(string.IsNullOrWhiteSpace(problema.Title));
    }

    [Fact]
    public async Task VerboNoAdmitido_DevuelveMetodoNoPermitidoConCuerpoCompleto()
    {
        var respuesta = await _cliente.PutAsJsonAsync("/api/v1/proveedores", new { });

        Assert.Equal(HttpStatusCode.MethodNotAllowed, respuesta.StatusCode);

        var problema = await respuesta.Content.ReadFromJsonAsync<ProblemaDetallado>();
        Assert.Equal(CodigosError.MetodoNoPermitido, problema!.Code);
        Assert.False(string.IsNullOrWhiteSpace(problema.CorrelationId));
    }

    [Fact]
    public async Task CuerpoQueNoSePuedeInterpretar_DevuelveSolicitudMalFormada()
    {
        using var contenido = new StringContent(
            "{ \"crCporUSD\": \"esto no es un numero\" }",
            Encoding.UTF8,
            "application/json");

        var respuesta = await _cliente.PostAsync(
            new Uri("/api/v1/tipos-cambio", UriKind.Relative),
            contenido);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);

        var problema = await respuesta.Content.ReadFromJsonAsync<ProblemaDetallado>();
        Assert.Equal(CodigosError.SolicitudInvalida, problema!.Code);
        Assert.False(string.IsNullOrWhiteSpace(problema.CorrelationId));
    }

    [Fact]
    public async Task IdentificadorConFormatoInvalido_NoProduceUnErrorInterno()
    {
        // La restricción de ruta exige un GUID, así que la petición ni siquiera entra.
        var respuesta = await _cliente.GetAsync(
            new Uri("/api/v1/proveedores/no-es-un-guid", UriKind.Relative));

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    [Theory]
    [InlineData("/api/v1/inventado")]
    [InlineData("/api/v1/proveedores/00000000-0000-0000-0000-000000000000")]
    public async Task NingunErrorRevelaDetallesInternos(string ruta)
    {
        var respuesta = await _cliente.GetAsync(new Uri(ruta, UriKind.Relative));
        var cuerpo = await respuesta.Content.ReadAsStringAsync();

        // Ni trazas de pila, ni rutas de archivos, ni nombres de tablas o consultas.
        Assert.DoesNotContain("Exception", cuerpo, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("at Licitaciones.", cuerpo, StringComparison.Ordinal);
        Assert.DoesNotContain("SELECT", cuerpo, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Password", cuerpo, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Forma mínima de una página, para comprobarla en cualquier recurso.</summary>
    private sealed record PaginaDePrueba(
        List<object> Elementos,
        int Pagina,
        int Tamano,
        int Total,
        int TotalPaginas);

    /// <summary>Forma de las respuestas de error, con las extensiones propias del proyecto.</summary>
    private sealed record ProblemaDetallado(
        string Title,
        int Status,
        string Detail,
        string Code,
        string CorrelationId);
}
