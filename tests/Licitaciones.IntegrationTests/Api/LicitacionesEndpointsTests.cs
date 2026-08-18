using System.Net;
using System.Net.Http.Json;
using Licitaciones.Application.Comun;
using Licitaciones.Application.Licitaciones;
using Licitaciones.Domain.Licitaciones;
using Licitaciones.IntegrationTests.Apoyo;

namespace Licitaciones.IntegrationTests.Api;

/// <summary>
/// Verifica los endpoints de licitaciones contra la API real y PostgreSQL real, incluidas
/// las transiciones de estado prohibidas (HU-015, HU-016, HU-017).
/// </summary>
[Collection(PostgresCollection.Nombre)]
public sealed class LicitacionesEndpointsTests : IDisposable
{
    private static readonly DateTimeOffset CierreFuturo = new(2027, 6, 30, 17, 0, 0, TimeSpan.Zero);

    private readonly ApiFactory _api;
    private readonly HttpClient _cliente;

    public LicitacionesEndpointsTests(PostgresFixture postgres)
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
    public async Task Crear_DevuelveCreadaEnBorradorConSuUbicacion()
    {
        var respuesta = await _cliente.PostAsJsonAsync("/api/v1/licitaciones", NuevaLicitacion());

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.NotNull(respuesta.Headers.Location);

        var creada = await respuesta.Content.ReadFromJsonAsync<LicitacionResponse>();
        Assert.Equal(EstadoLicitacion.Borrador, creada!.Estado);
        Assert.False(creada.CerradaFuncionalmente);
    }

    [Fact]
    public async Task Crear_ConCodigoDuplicadoIgnorandoCaja_DevuelveConflicto()
    {
        var peticion = NuevaLicitacion();
        await _cliente.PostAsJsonAsync("/api/v1/licitaciones", peticion);

        var repetida = peticion with { Codigo = $"  {peticion.Codigo.ToLowerInvariant()}  " };
        var respuesta = await _cliente.PostAsJsonAsync("/api/v1/licitaciones", repetida);

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);

        var problema = await respuesta.Content.ReadFromJsonAsync<ProblemaDetallado>();
        Assert.Equal(CodigosError.CodigoLicitacionDuplicado, problema!.Code);
    }

    [Fact]
    public async Task Crear_ConPresupuestoCero_DevuelveEntidadNoProcesable()
    {
        var respuesta = await _cliente.PostAsJsonAsync(
            "/api/v1/licitaciones",
            NuevaLicitacion() with { PresupuestoEstimadoCRC = 0m });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);
    }

    [Fact]
    public async Task CambiarEstado_DeBorradorAPublicada_EsAceptado()
    {
        var creada = await CrearAsync();

        var respuesta = await _cliente.PatchAsJsonAsync(
            $"/api/v1/licitaciones/{creada.Id}/estado",
            new CambiarEstadoRequest(EstadoLicitacion.Publicada));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        var actualizada = await respuesta.Content.ReadFromJsonAsync<LicitacionResponse>();
        Assert.Equal(EstadoLicitacion.Publicada, actualizada!.Estado);
    }

    [Fact]
    public async Task CambiarEstado_DePublicadaABorrador_DevuelveConflicto()
    {
        var creada = await CrearAsync();
        await _cliente.PatchAsJsonAsync(
            $"/api/v1/licitaciones/{creada.Id}/estado",
            new CambiarEstadoRequest(EstadoLicitacion.Publicada));

        var respuesta = await _cliente.PatchAsJsonAsync(
            $"/api/v1/licitaciones/{creada.Id}/estado",
            new CambiarEstadoRequest(EstadoLicitacion.Borrador));

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);

        var problema = await respuesta.Content.ReadFromJsonAsync<ProblemaDetallado>();
        Assert.Equal(CodigosError.TransicionInvalida, problema!.Code);
        Assert.Equal("Transición de estado no permitida.", problema.Detail);
    }

    [Fact]
    public async Task CambiarEstado_DeCerradaAPublicada_DevuelveConflicto()
    {
        var creada = await CrearAsync();
        await _cliente.PatchAsJsonAsync(
            $"/api/v1/licitaciones/{creada.Id}/estado",
            new CambiarEstadoRequest(EstadoLicitacion.Cerrada));

        var respuesta = await _cliente.PatchAsJsonAsync(
            $"/api/v1/licitaciones/{creada.Id}/estado",
            new CambiarEstadoRequest(EstadoLicitacion.Publicada));

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
    }

    [Fact]
    public async Task MejorOferta_SinOfertas_DevuelveLaEtiquetaSinOfertasValidas()
    {
        var creada = await CrearAsync();

        var mejor = await _cliente.GetFromJsonAsync<MejorOfertaResponse>(
            $"/api/v1/licitaciones/{creada.Id}/mejor-oferta");

        Assert.Null(mejor!.Oferta);
        Assert.Equal("Sin ofertas válidas", mejor.Clasificacion);
    }

    [Fact]
    public async Task Obtener_UnaLicitacionInexistente_DevuelveNoEncontrada()
    {
        var respuesta = await _cliente.GetAsync($"/api/v1/licitaciones/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    [Fact]
    public async Task Eliminar_DevuelveSinContenido()
    {
        var creada = await CrearAsync();

        var respuesta = await _cliente.DeleteAsync($"/api/v1/licitaciones/{creada.Id}");

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
    }

    private static CrearLicitacionRequest NuevaLicitacion() => new(
        $"LIC-{Guid.NewGuid():N}"[..12],
        "Compra de equipo de cómputo",
        1_000_000.00m,
        CierreFuturo);

    private async Task<LicitacionResponse> CrearAsync()
    {
        var respuesta = await _cliente.PostAsJsonAsync("/api/v1/licitaciones", NuevaLicitacion());

        return (await respuesta.Content.ReadFromJsonAsync<LicitacionResponse>())!;
    }

    /// <summary>Forma de las respuestas de error, con las extensiones propias del proyecto.</summary>
    private sealed record ProblemaDetallado(
        string Title,
        int Status,
        string Detail,
        string Code,
        string CorrelationId);
}
