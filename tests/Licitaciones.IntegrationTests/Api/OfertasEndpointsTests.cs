using System.Net;
using System.Net.Http.Json;
using Licitaciones.Application.Comun;
using Licitaciones.Application.Licitaciones;
using Licitaciones.Application.Ofertas;
using Licitaciones.Application.Proveedores;
using Licitaciones.Domain.Licitaciones;
using Licitaciones.IntegrationTests.Apoyo;

namespace Licitaciones.IntegrationTests.Api;

/// <summary>
/// Verifica la matriz de operaciones sobre ofertas contra la API real y PostgreSQL real:
/// qué se admite y qué se rechaza según el estado de la licitación (HU-018 a HU-022).
/// </summary>
[Collection(PostgresCollection.Nombre)]
public sealed class OfertasEndpointsTests : IDisposable
{
    private static readonly DateTimeOffset CierreFuturo = new(2027, 6, 30, 17, 0, 0, TimeSpan.Zero);

    private readonly ApiFactory _api;
    private readonly HttpClient _cliente;

    public OfertasEndpointsTests(PostgresFixture postgres)
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

    [Fact]
    public async Task Crear_SobreLicitacionPublicada_DevuelveCreada()
    {
        var licitacion = await CrearLicitacionAsync(publicar: true);
        var proveedor = await CrearProveedorAsync();

        var respuesta = await _cliente.PostAsJsonAsync(
            "/api/v1/ofertas",
            new CrearOfertaRequest(licitacion.Id, proveedor.Id, 800_000m));

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);

        var creada = await respuesta.Content.ReadFromJsonAsync<OfertaResponse>();
        Assert.Equal(800_000m, creada!.MontoOfertadoCRC);
        Assert.Equal(licitacion.Codigo, creada.CodigoLicitacion);
        Assert.Equal(proveedor.Nombre, creada.NombreProveedor);
    }

    [Fact]
    public async Task Crear_SobreLicitacionEnBorrador_DevuelveConflicto()
    {
        var licitacion = await CrearLicitacionAsync(publicar: false);
        var proveedor = await CrearProveedorAsync();

        var respuesta = await _cliente.PostAsJsonAsync(
            "/api/v1/ofertas",
            new CrearOfertaRequest(licitacion.Id, proveedor.Id, 800_000m));

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);

        var problema = await respuesta.Content.ReadFromJsonAsync<ProblemaDetallado>();
        Assert.Equal(CodigosError.LicitacionNoPublicada, problema!.Code);
        Assert.Equal("La licitación no está publicada.", problema.Detail);
    }

    [Fact]
    public async Task Crear_SobreLicitacionCerrada_DevuelveConflicto()
    {
        var licitacion = await CrearLicitacionAsync(publicar: true);
        var proveedor = await CrearProveedorAsync();
        await _cliente.PatchAsJsonAsync(
            $"/api/v1/licitaciones/{licitacion.Id}/estado",
            new CambiarEstadoRequest(EstadoLicitacion.Cerrada));

        var respuesta = await _cliente.PostAsJsonAsync(
            "/api/v1/ofertas",
            new CrearOfertaRequest(licitacion.Id, proveedor.Id, 800_000m));

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);

        var problema = await respuesta.Content.ReadFromJsonAsync<ProblemaDetallado>();
        Assert.Equal(CodigosError.LicitacionCerrada, problema!.Code);
    }

    [Fact]
    public async Task Crear_UnaSegundaOfertaDelMismoProveedor_DevuelveConflicto()
    {
        var licitacion = await CrearLicitacionAsync(publicar: true);
        var proveedor = await CrearProveedorAsync();
        await _cliente.PostAsJsonAsync(
            "/api/v1/ofertas",
            new CrearOfertaRequest(licitacion.Id, proveedor.Id, 800_000m));

        var respuesta = await _cliente.PostAsJsonAsync(
            "/api/v1/ofertas",
            new CrearOfertaRequest(licitacion.Id, proveedor.Id, 700_000m));

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);

        var problema = await respuesta.Content.ReadFromJsonAsync<ProblemaDetallado>();
        Assert.Equal(CodigosError.OfertaDuplicada, problema!.Code);
    }

    [Fact]
    public async Task Crear_ConMontoSuperiorAlPresupuesto_DevuelveEntidadNoProcesable()
    {
        var licitacion = await CrearLicitacionAsync(publicar: true);
        var proveedor = await CrearProveedorAsync();

        var respuesta = await _cliente.PostAsJsonAsync(
            "/api/v1/ofertas",
            new CrearOfertaRequest(licitacion.Id, proveedor.Id, 1_000_000.01m));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);

        var problema = await respuesta.Content.ReadFromJsonAsync<ProblemaDetallado>();
        Assert.Equal(CodigosError.OfertaSuperaPresupuesto, problema!.Code);
    }

    [Fact]
    public async Task Crear_ConMontoIgualAlPresupuesto_DevuelveCreada()
    {
        var licitacion = await CrearLicitacionAsync(publicar: true);
        var proveedor = await CrearProveedorAsync();

        var respuesta = await _cliente.PostAsJsonAsync(
            "/api/v1/ofertas",
            new CrearOfertaRequest(licitacion.Id, proveedor.Id, 1_000_000.00m));

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
    }

    [Fact]
    public async Task CrearDesdeLaRutaDeLaLicitacion_DevuelveCreada()
    {
        var licitacion = await CrearLicitacionAsync(publicar: true);
        var proveedor = await CrearProveedorAsync();

        var respuesta = await _cliente.PostAsJsonAsync(
            $"/api/v1/licitaciones/{licitacion.Id}/ofertas",
            new CrearOfertaEnLicitacionRequest(proveedor.Id, 750_000m));

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
    }

    [Fact]
    public async Task Editar_UnaOfertaDeLicitacionCerrada_DevuelveConflicto()
    {
        var (licitacion, oferta) = await CrearLicitacionConOfertaAsync();
        await _cliente.PatchAsJsonAsync(
            $"/api/v1/licitaciones/{licitacion.Id}/estado",
            new CambiarEstadoRequest(EstadoLicitacion.Cerrada));

        var respuesta = await _cliente.PutAsJsonAsync(
            $"/api/v1/ofertas/{oferta.Id}",
            new ActualizarOfertaRequest(700_000m));

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);

        var problema = await respuesta.Content.ReadFromJsonAsync<ProblemaDetallado>();
        Assert.Equal(CodigosError.OfertaInmutable, problema!.Code);
        Assert.Equal("Las ofertas de licitaciones cerradas no pueden modificarse.", problema.Detail);
    }

    [Fact]
    public async Task Eliminar_UnaOfertaDeLicitacionCerrada_DevuelveConflictoYLaConserva()
    {
        var (licitacion, oferta) = await CrearLicitacionConOfertaAsync();
        await _cliente.PatchAsJsonAsync(
            $"/api/v1/licitaciones/{licitacion.Id}/estado",
            new CambiarEstadoRequest(EstadoLicitacion.Cerrada));

        var respuesta = await _cliente.DeleteAsync($"/api/v1/ofertas/{oferta.Id}");

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);

        // La evidencia se conserva.
        var sigue = await _cliente.GetAsync($"/api/v1/ofertas/{oferta.Id}");
        Assert.Equal(HttpStatusCode.OK, sigue.StatusCode);
    }

    [Fact]
    public async Task Editar_MientrasLaLicitacionSigueVigente_EsAceptado()
    {
        var (_, oferta) = await CrearLicitacionConOfertaAsync();

        var respuesta = await _cliente.PutAsJsonAsync(
            $"/api/v1/ofertas/{oferta.Id}",
            new ActualizarOfertaRequest(700_000m));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        var actualizada = await respuesta.Content.ReadFromJsonAsync<OfertaResponse>();
        Assert.Equal(700_000m, actualizada!.MontoOfertadoCRC);
    }

    [Fact]
    public async Task Eliminar_MientrasLaLicitacionSigueVigente_EsAceptado()
    {
        var (_, oferta) = await CrearLicitacionConOfertaAsync();

        var respuesta = await _cliente.DeleteAsync($"/api/v1/ofertas/{oferta.Id}");

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
    }

    [Fact]
    public async Task MejorOferta_DevuelveLaDeMenorMontoConSuClasificacion()
    {
        var licitacion = await CrearLicitacionAsync(publicar: true);
        var primero = await CrearProveedorAsync();
        var segundo = await CrearProveedorAsync();
        await _cliente.PostAsJsonAsync(
            "/api/v1/ofertas",
            new CrearOfertaRequest(licitacion.Id, primero.Id, 950_000m));
        await _cliente.PostAsJsonAsync(
            "/api/v1/ofertas",
            new CrearOfertaRequest(licitacion.Id, segundo.Id, 900_000m));

        var mejor = await _cliente.GetFromJsonAsync<MejorOfertaResponse>(
            $"/api/v1/licitaciones/{licitacion.Id}/mejor-oferta");

        Assert.Equal(900_000m, mejor!.Oferta!.MontoOfertadoCRC);
        Assert.Equal(10m, mejor.PorcentajeAhorro);
        Assert.Equal("Oferta conveniente", mejor.Clasificacion);
    }

    [Fact]
    public async Task Listar_FiltraPorLicitacion()
    {
        var (licitacion, _) = await CrearLicitacionConOfertaAsync();

        var pagina = await _cliente.GetFromJsonAsync<PagedResponse<OfertaResponse>>(
            $"/api/v1/ofertas?licitacionId={licitacion.Id}");

        Assert.Equal(1, pagina!.Total);
    }

    private async Task<LicitacionResponse> CrearLicitacionAsync(bool publicar)
    {
        var creada = await (await _cliente.PostAsJsonAsync(
            "/api/v1/licitaciones",
            new CrearLicitacionRequest(
                $"LIC-{Guid.NewGuid():N}"[..12],
                "Licitación de prueba",
                1_000_000.00m,
                CierreFuturo))).Content.ReadFromJsonAsync<LicitacionResponse>();

        if (publicar)
        {
            await _cliente.PatchAsJsonAsync(
                $"/api/v1/licitaciones/{creada!.Id}/estado",
                new CambiarEstadoRequest(EstadoLicitacion.Publicada));
        }

        return creada!;
    }

    private async Task<ProveedorResponse> CrearProveedorAsync() =>
        (await (await _cliente.PostAsJsonAsync(
            "/api/v1/proveedores",
            new CrearProveedorRequest($"Proveedor {Guid.NewGuid():N}"[..25])))
            .Content.ReadFromJsonAsync<ProveedorResponse>())!;

    private async Task<(LicitacionResponse Licitacion, OfertaResponse Oferta)> CrearLicitacionConOfertaAsync()
    {
        var licitacion = await CrearLicitacionAsync(publicar: true);
        var proveedor = await CrearProveedorAsync();
        var oferta = await (await _cliente.PostAsJsonAsync(
            "/api/v1/ofertas",
            new CrearOfertaRequest(licitacion.Id, proveedor.Id, 800_000m)))
            .Content.ReadFromJsonAsync<OfertaResponse>();

        return (licitacion, oferta!);
    }

    /// <summary>Forma de las respuestas de error, con las extensiones propias del proyecto.</summary>
    private sealed record ProblemaDetallado(
        string Title,
        int Status,
        string Detail,
        string Code,
        string CorrelationId);
}
