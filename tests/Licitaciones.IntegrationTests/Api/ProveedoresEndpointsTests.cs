using System.Net;
using System.Net.Http.Json;
using Licitaciones.Application.Comun;
using Licitaciones.Application.Proveedores;
using Licitaciones.IntegrationTests.Apoyo;

namespace Licitaciones.IntegrationTests.Api;

/// <summary>
/// Verifica los endpoints de proveedores contra la API real y PostgreSQL real: códigos
/// HTTP, cabecera de ubicación y respuestas de error (HU-013, HU-014).
/// </summary>
[Collection(PostgresCollection.Nombre)]
public sealed class ProveedoresEndpointsTests : IDisposable
{
    private readonly ApiFactory _api;
    private readonly HttpClient _cliente;

    public ProveedoresEndpointsTests(PostgresFixture postgres)
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
    public async Task Crear_DevuelveCreadoConLaUbicacionDelRecurso()
    {
        var nombre = $"Endpoint {Guid.NewGuid():N}"[..24];

        var respuesta = await _cliente.PostAsJsonAsync(
            "/api/v1/proveedores",
            new CrearProveedorRequest(nombre));

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.NotNull(respuesta.Headers.Location);

        var creado = await respuesta.Content.ReadFromJsonAsync<ProveedorResponse>();
        Assert.Equal(nombre, creado!.Nombre);
    }

    [Fact]
    public async Task Crear_ConNombreDuplicado_DevuelveConflicto()
    {
        var nombre = $"Repetido {Guid.NewGuid():N}"[..24];
        await _cliente.PostAsJsonAsync("/api/v1/proveedores", new CrearProveedorRequest(nombre));

        var respuesta = await _cliente.PostAsJsonAsync(
            "/api/v1/proveedores",
            new CrearProveedorRequest(nombre.ToUpperInvariant()));

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);

        var problema = await respuesta.Content.ReadFromJsonAsync<ProblemaDetallado>();
        Assert.Equal(CodigosError.ProveedorDuplicado, problema!.Code);
        Assert.Equal("Ya existe un proveedor con ese nombre.", problema.Detail);
        Assert.False(string.IsNullOrWhiteSpace(problema.CorrelationId));
    }

    [Fact]
    public async Task Crear_ConCaracteresNoAdmitidos_DevuelveEntidadNoProcesable()
    {
        var respuesta = await _cliente.PostAsJsonAsync(
            "/api/v1/proveedores",
            new CrearProveedorRequest("Empresa@Central"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);

        var problema = await respuesta.Content.ReadFromJsonAsync<ProblemaDetallado>();
        Assert.Equal(CodigosError.NombreProveedorInvalido, problema!.Code);
    }

    [Fact]
    public async Task Obtener_UnProveedorInexistente_DevuelveNoEncontrado()
    {
        var respuesta = await _cliente.GetAsync($"/api/v1/proveedores/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    [Fact]
    public async Task Eliminar_DevuelveSinContenidoYDejaDeListarlo()
    {
        var nombre = $"Baja {Guid.NewGuid():N}"[..24];
        var creado = await (await _cliente.PostAsJsonAsync(
            "/api/v1/proveedores",
            new CrearProveedorRequest(nombre))).Content.ReadFromJsonAsync<ProveedorResponse>();

        var respuesta = await _cliente.DeleteAsync($"/api/v1/proveedores/{creado!.Id}");

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await _cliente.GetAsync($"/api/v1/proveedores/{creado.Id}")).StatusCode);
    }

    [Fact]
    public async Task Listar_DevuelveLaEnvolturaConTotalYPaginas()
    {
        await _cliente.PostAsJsonAsync(
            "/api/v1/proveedores",
            new CrearProveedorRequest($"Listado {Guid.NewGuid():N}"[..24]));

        var pagina = await _cliente.GetFromJsonAsync<PagedResponse<ProveedorResponse>>(
            "/api/v1/proveedores?pagina=1&tamano=5");

        Assert.NotNull(pagina);
        Assert.True(pagina.Total >= 1);
        Assert.Equal(1, pagina.Pagina);
        Assert.Equal(5, pagina.Tamano);
    }

    [Fact]
    public async Task Actualizar_UnProveedorInexistente_DevuelveNoEncontrado()
    {
        var respuesta = await _cliente.PutAsJsonAsync(
            $"/api/v1/proveedores/{Guid.NewGuid()}",
            new ActualizarProveedorRequest("Cualquiera"));

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    /// <summary>Forma de las respuestas de error, con las extensiones propias del proyecto.</summary>
    private sealed record ProblemaDetallado(
        string Title,
        int Status,
        string Detail,
        string Code,
        string CorrelationId);
}
