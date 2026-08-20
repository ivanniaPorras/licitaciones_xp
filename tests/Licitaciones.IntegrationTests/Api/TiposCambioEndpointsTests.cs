using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Licitaciones.Application.Comun;
using Licitaciones.Application.Moneda;
using Licitaciones.IntegrationTests.Apoyo;

namespace Licitaciones.IntegrationTests.Api;

/// <summary>
/// Verifica la administración de tasas y la conversión a dólares contra la API real y
/// PostgreSQL real. La exclusividad del registro activo depende de un índice único
/// parcial y de una transacción, así que solo puede comprobarse aquí (HU-026, HU-027).
/// </summary>
[Collection(PostgresCollection.Nombre)]
public sealed class TiposCambioEndpointsTests : IDisposable
{
    private static readonly DateTimeOffset Vigencia = new(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly ApiFactory _api;
    private readonly HttpClient _cliente;

    public TiposCambioEndpointsTests(PostgresFixture postgres)
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
    public async Task Crear_DevuelveCreadoYLaUbicacionDelRecurso()
    {
        var respuesta = await _cliente.PostAsJsonAsync(
            "/api/v1/tipos-cambio",
            new CrearTipoCambioRequest(525.7500m, Vigencia));

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.NotNull(respuesta.Headers.Location);

        var creado = await respuesta.Content.ReadFromJsonAsync<TipoCambioResponse>();
        Assert.Equal(525.7500m, creado!.CRCporUSD);
        Assert.False(creado.Activo);

        await EliminarAsync(creado.Id);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-512.50)]
    public async Task Crear_ConTasaNoPositiva_DevuelveEntidadNoProcesable(decimal tasa)
    {
        var respuesta = await _cliente.PostAsJsonAsync(
            "/api/v1/tipos-cambio",
            new CrearTipoCambioRequest(tasa, Vigencia));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);

        var problema = await respuesta.Content.ReadFromJsonAsync<ProblemaDetallado>();
        Assert.Equal(CodigosError.TasaInvalida, problema!.Code);
    }

    [Fact]
    public async Task Obtener_UnaTasaInexistente_DevuelveNoEncontrado()
    {
        var respuesta = await _cliente.GetAsync(
            new Uri($"/api/v1/tipos-cambio/{Guid.NewGuid()}", UriKind.Relative));

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    [Fact]
    public async Task Vigente_DevuelveLaTasaDeLaSemilla()
    {
        var vigente = await _cliente.GetFromJsonAsync<TipoCambioResponse>("/api/v1/tipos-cambio/vigente");

        Assert.True(vigente!.Activo);
    }

    [Fact]
    public async Task Activar_DejaUnaUnicaTasaActivaEnLaBase()
    {
        var anterior = await ObtenerVigenteAsync();
        var nueva = await CrearAsync(530.0000m);

        var respuesta = await _cliente.PostAsync(
            new Uri($"/api/v1/tipos-cambio/{nueva.Id}/activar", UriKind.Relative),
            content: null);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        var todas = await _cliente.GetFromJsonAsync<List<TipoCambioResponse>>("/api/v1/tipos-cambio");
        Assert.Single(todas!, t => t.Activo);
        Assert.Equal(nueva.Id, todas!.Single(t => t.Activo).Id);

        // Se devuelve la tasa anterior a su lugar para no alterar a las demás pruebas.
        await _cliente.PostAsync(
            new Uri($"/api/v1/tipos-cambio/{anterior.Id}/activar", UriKind.Relative),
            content: null);
        await EliminarAsync(nueva.Id);
    }

    [Fact]
    public async Task Activar_UnaTasaInexistente_DevuelveNoEncontradoYNoTocaLaVigente()
    {
        var antes = await ObtenerVigenteAsync();

        var respuesta = await _cliente.PostAsync(
            new Uri($"/api/v1/tipos-cambio/{Guid.NewGuid()}/activar", UriKind.Relative),
            content: null);

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);

        var despues = await ObtenerVigenteAsync();
        Assert.Equal(antes.Id, despues.Id);
    }

    [Fact]
    public async Task Actualizar_CambiaLaTasaSinAlterarSuUso()
    {
        var creada = await CrearAsync(500.0000m);

        var respuesta = await _cliente.PutAsJsonAsync(
            $"/api/v1/tipos-cambio/{creada.Id}",
            new ActualizarTipoCambioRequest(505.5000m, Vigencia.AddMonths(1)));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        var actualizada = await respuesta.Content.ReadFromJsonAsync<TipoCambioResponse>();
        Assert.Equal(505.5000m, actualizada!.CRCporUSD);
        Assert.False(actualizada.Activo);

        await EliminarAsync(creada.Id);
    }

    [Fact]
    public async Task Eliminar_DevuelveSinContenidoYLaTasaDejaDeListarse()
    {
        var creada = await CrearAsync(499.0000m);

        var respuesta = await EliminarAsync(creada.Id);
        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);

        var todas = await _cliente.GetFromJsonAsync<List<TipoCambioResponse>>("/api/v1/tipos-cambio");
        Assert.DoesNotContain(todas!, t => t.Id == creada.Id);
    }

    [Fact]
    public async Task Conversion_DivideEntreLaTasaVigenteYDevuelveLaTasaUsada()
    {
        var vigente = await ObtenerVigenteAsync();
        const decimal MontoCRC = 1_250_000.00m;

        var conversion = await _cliente.GetFromJsonAsync<ConversionResponse>(
            $"/api/v1/tipos-cambio/conversion?monto={MontoCRC.ToString(CultureInfo.InvariantCulture)}");

        var esperado = Math.Round(MontoCRC / vigente.CRCporUSD, 2, MidpointRounding.AwayFromZero);
        Assert.Equal(esperado, conversion!.MontoUSD);
        Assert.Equal(vigente.CRCporUSD, conversion.CRCporUSD);
        Assert.Equal(vigente.FechaVigencia, conversion.FechaVigencia);

        // Los colones viajan intactos: la conversión no toca el valor almacenado.
        Assert.Equal(MontoCRC, conversion.MontoCRC);
    }

    [Fact]
    public async Task Conversion_ConMontoNoPositivo_DevuelveEntidadNoProcesable()
    {
        var respuesta = await _cliente.GetAsync(
            new Uri("/api/v1/tipos-cambio/conversion?monto=0", UriKind.Relative));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);

        var problema = await respuesta.Content.ReadFromJsonAsync<ProblemaDetallado>();
        Assert.Equal(CodigosError.MontoInvalido, problema!.Code);
    }

    private async Task<TipoCambioResponse> ObtenerVigenteAsync() =>
        (await _cliente.GetFromJsonAsync<TipoCambioResponse>("/api/v1/tipos-cambio/vigente"))!;

    private async Task<TipoCambioResponse> CrearAsync(decimal tasa) =>
        (await (await _cliente.PostAsJsonAsync(
            "/api/v1/tipos-cambio",
            new CrearTipoCambioRequest(tasa, Vigencia)))
            .Content.ReadFromJsonAsync<TipoCambioResponse>())!;

    private Task<HttpResponseMessage> EliminarAsync(Guid id) =>
        _cliente.DeleteAsync(new Uri($"/api/v1/tipos-cambio/{id}", UriKind.Relative));

    /// <summary>Forma de las respuestas de error, con las extensiones propias del proyecto.</summary>
    private sealed record ProblemaDetallado(
        string Title,
        int Status,
        string Detail,
        string Code,
        string CorrelationId);
}
