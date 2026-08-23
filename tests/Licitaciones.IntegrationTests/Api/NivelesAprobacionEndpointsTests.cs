using System.Net;
using System.Net.Http.Json;
using Licitaciones.Application.Aprobacion;
using Licitaciones.Application.Comun;
using Licitaciones.Application.Licitaciones;
using Licitaciones.Application.Ofertas;
using Licitaciones.Application.Proveedores;
using Licitaciones.Domain.Licitaciones;
using Licitaciones.IntegrationTests.Apoyo;

namespace Licitaciones.IntegrationTests.Api;

/// <summary>
/// Verifica que el aprobador se resuelva contra la tabla sembrada y que la mejor oferta
/// lo incluya, contra la API real y PostgreSQL real (HU-024, HU-025).
/// </summary>
[Collection(PostgresCollection.Nombre)]
public sealed class NivelesAprobacionEndpointsTests : IDisposable
{
    private static readonly DateTimeOffset CierreFuturo = new(2027, 6, 30, 17, 0, 0, TimeSpan.Zero);

    private readonly ApiFactory _api;
    private readonly HttpClient _cliente;

    public NivelesAprobacionEndpointsTests(PostgresFixture postgres)
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

    [Theory]
    [InlineData(0.01, "Encargado de área")]
    [InlineData(999999.99, "Encargado de área")]
    [InlineData(1000000.00, "Gerencia")]
    [InlineData(9999999.99, "Gerencia")]
    [InlineData(10000000.00, "Junta Directiva")]
    [InlineData(50000000.00, "Junta Directiva")]
    public async Task Aprobador_SeResuelveContraLaTablaSembrada(decimal monto, string esperado)
    {
        var nivel = await _cliente.GetFromJsonAsync<NivelAprobacionResponse>(
            $"/api/v1/niveles-aprobacion/aprobador?monto={monto.ToString(System.Globalization.CultureInfo.InvariantCulture)}");

        Assert.Equal(esperado, nivel!.Aprobador);
    }

    [Fact]
    public async Task Listar_DevuelveLosTresNivelesDeLaSemilla()
    {
        var niveles = await _cliente.GetFromJsonAsync<PagedResponse<NivelAprobacionResponse>>(
            "/api/v1/niveles-aprobacion");

        // Se comprueba que estén los tres niveles sembrados. No se afirma que haya un
        // único rango abierto: las pruebas comparten la misma base y otras insertan
        // niveles por el contexto, saltándose el servicio. Esa regla la verifica
        // Crear_UnSegundoRangoAbierto_DevuelveConflicto.
        var aprobadores = niveles!.Elementos.Select(n => n.Aprobador).ToList();
        Assert.Contains("Encargado de área", aprobadores);
        Assert.Contains("Gerencia", aprobadores);
        Assert.Contains("Junta Directiva", aprobadores);
    }

    [Fact]
    public async Task Crear_UnRangoQueSeTraslapa_DevuelveConflicto()
    {
        var respuesta = await _cliente.PostAsJsonAsync(
            "/api/v1/niveles-aprobacion",
            new CrearNivelAprobacionRequest(500_000.00m, 2_000_000.00m, "Intruso"));

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);

        var problema = await respuesta.Content.ReadFromJsonAsync<ProblemaDetallado>();
        Assert.Equal(CodigosError.RangoTraslapado, problema!.Code);
        Assert.Equal("El rango se traslapa con un nivel existente.", problema.Detail);
    }

    [Fact]
    public async Task Crear_UnSegundoRangoAbierto_DevuelveConflicto()
    {
        var respuesta = await _cliente.PostAsJsonAsync(
            "/api/v1/niveles-aprobacion",
            new CrearNivelAprobacionRequest(100_000_000.00m, null, "Asamblea"));

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);

        var problema = await respuesta.Content.ReadFromJsonAsync<ProblemaDetallado>();
        Assert.Equal(CodigosError.RangoAbiertoDuplicado, problema!.Code);
    }

    [Fact]
    public async Task Crear_ConMaximoMenorQueElMinimo_DevuelveEntidadNoProcesable()
    {
        var respuesta = await _cliente.PostAsJsonAsync(
            "/api/v1/niveles-aprobacion",
            new CrearNivelAprobacionRequest(2_000_000.00m, 1_000_000.00m, "Alguien"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);
    }

    [Fact]
    public async Task MejorOferta_IncluyeElAprobadorQueCorrespondeAlMonto()
    {
        var licitacion = await CrearLicitacionPublicadaAsync(presupuesto: 20_000_000.00m);
        var proveedor = await CrearProveedorAsync();
        await _cliente.PostAsJsonAsync(
            "/api/v1/ofertas",
            new CrearOfertaRequest(licitacion.Id, proveedor.Id, 15_000_000.00m));

        var mejor = await _cliente.GetFromJsonAsync<MejorOfertaResponse>(
            $"/api/v1/licitaciones/{licitacion.Id}/mejor-oferta");

        // 15 000 000 cae en el rango abierto de la semilla.
        Assert.Equal("Junta Directiva", mejor!.Aprobador);
        Assert.Equal(25m, mejor.PorcentajeAhorro);
        Assert.Equal("Oferta conveniente", mejor.Clasificacion);
    }

    [Fact]
    public async Task MejorOferta_SinOfertas_NoDevuelveAprobador()
    {
        var licitacion = await CrearLicitacionPublicadaAsync(presupuesto: 1_000_000.00m);

        var mejor = await _cliente.GetFromJsonAsync<MejorOfertaResponse>(
            $"/api/v1/licitaciones/{licitacion.Id}/mejor-oferta");

        Assert.Null(mejor!.Aprobador);
        Assert.Equal("Sin ofertas válidas", mejor.Clasificacion);
    }

    private async Task<LicitacionResponse> CrearLicitacionPublicadaAsync(decimal presupuesto)
    {
        var creada = await (await _cliente.PostAsJsonAsync(
            "/api/v1/licitaciones",
            new CrearLicitacionRequest(
                $"LIC-{Guid.NewGuid():N}"[..12],
                "Licitación de prueba",
                presupuesto,
                CierreFuturo))).Content.ReadFromJsonAsync<LicitacionResponse>();

        await _cliente.PatchAsJsonAsync(
            $"/api/v1/licitaciones/{creada!.Id}/estado",
            new CambiarEstadoRequest(EstadoLicitacion.Publicada));

        return creada;
    }

    private async Task<ProveedorResponse> CrearProveedorAsync() =>
        (await (await _cliente.PostAsJsonAsync(
            "/api/v1/proveedores",
            new CrearProveedorRequest($"Proveedor {Guid.NewGuid():N}"[..25])))
            .Content.ReadFromJsonAsync<ProveedorResponse>())!;

    /// <summary>Forma de las respuestas de error, con las extensiones propias del proyecto.</summary>
    private sealed record ProblemaDetallado(
        string Title,
        int Status,
        string Detail,
        string Code,
        string CorrelationId);
}
