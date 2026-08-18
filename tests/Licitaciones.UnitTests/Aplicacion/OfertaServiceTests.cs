using Licitaciones.Application.Comun;
using Licitaciones.Application.Ofertas;
using Licitaciones.Domain.Licitaciones;
using Licitaciones.Domain.Ofertas;
using Licitaciones.Domain.Proveedores;
using Licitaciones.UnitTests.Apoyo;
using Licitaciones.UnitTests.Apoyo.Dobles;

namespace Licitaciones.UnitTests.Aplicacion;

/// <summary>
/// Verifica las reglas del servicio de ofertas. Es el módulo con mayor densidad de
/// reglas: monto positivo, oferta no superior al presupuesto, una sola oferta por
/// proveedor y licitación, solo sobre licitaciones publicadas y vigentes, e inmutabilidad
/// de las ofertas de licitaciones cerradas (HU-018 a HU-022).
/// </summary>
public sealed class OfertaServiceTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 8, 20, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Cierre = new(2026, 12, 31, 17, 0, 0, TimeSpan.Zero);

    private readonly RepositorioLicitacionesEnMemoria _licitaciones = new();
    private readonly RepositorioProveedoresEnMemoria _proveedores = new();
    private readonly RepositorioOfertasEnMemoria _ofertas = new();
    private readonly UnidadDeTrabajoFalsa _unidad = new();
    private readonly RelojFalso _reloj = new(Ahora);

    private OfertaService CrearServicio() =>
        new(_ofertas, _licitaciones, _proveedores, _unidad, _reloj);

    private Licitacion SembrarPublicada(decimal presupuesto = 1_000_000m)
    {
        var licitacion = Licitacion.Crear("LIC-100", "Publicada", presupuesto, Cierre);
        licitacion.CambiarEstado(EstadoLicitacion.Publicada);
        _licitaciones.Sembrar(licitacion);
        return licitacion;
    }

    private Proveedor SembrarProveedor(string nombre = "Empresa Central")
    {
        var proveedor = Proveedor.Crear(nombre);
        _proveedores.Sembrar(proveedor);
        return proveedor;
    }

    // ---- HU-018 · Registrar una oferta válida ----

    [Fact]
    public async Task Crear_SobreLicitacionPublicadaYVigente_EsAceptada()
    {
        var licitacion = SembrarPublicada();
        var proveedor = SembrarProveedor();

        var resultado = await CrearServicio().CrearAsync(
            new CrearOfertaRequest(licitacion.Id, proveedor.Id, 800_000m));

        Assert.True(resultado.EsCorrecto);
        Assert.Equal(800_000m, resultado.Valor!.MontoOfertadoCRC);
        Assert.Single(_ofertas.Contenido);
    }

    [Fact]
    public async Task Crear_RegistraElInstanteDelRelojInyectado()
    {
        var licitacion = SembrarPublicada();
        var proveedor = SembrarProveedor();

        await CrearServicio().CrearAsync(new CrearOfertaRequest(licitacion.Id, proveedor.Id, 800_000m));

        Assert.Equal(Ahora, _ofertas.Contenido[0].FechaRegistro);
    }

    [Fact]
    public async Task Crear_SobreUnaLicitacionInexistente_DevuelveNoEncontrada()
    {
        var proveedor = SembrarProveedor();

        var resultado = await CrearServicio().CrearAsync(
            new CrearOfertaRequest(Guid.NewGuid(), proveedor.Id, 800_000m));

        Assert.Equal(CodigosError.LicitacionNoEncontrada, resultado.Error!.Codigo);
    }

    [Fact]
    public async Task Crear_ConUnProveedorInexistente_DevuelveNoEncontrado()
    {
        var licitacion = SembrarPublicada();

        var resultado = await CrearServicio().CrearAsync(
            new CrearOfertaRequest(licitacion.Id, Guid.NewGuid(), 800_000m));

        Assert.Equal(CodigosError.ProveedorNoEncontrado, resultado.Error!.Codigo);
    }

    // ---- HU-019 · Oferta no superior al presupuesto ----

    [Fact]
    public async Task Crear_ConMontoSuperiorAlPresupuesto_EsRechazada()
    {
        var licitacion = SembrarPublicada(1_000_000m);
        var proveedor = SembrarProveedor();

        var resultado = await CrearServicio().CrearAsync(
            new CrearOfertaRequest(licitacion.Id, proveedor.Id, 1_000_000.01m));

        Assert.False(resultado.EsCorrecto);
        Assert.Equal(CodigosError.OfertaSuperaPresupuesto, resultado.Error!.Codigo);
        Assert.Equal(TipoError.Validacion, resultado.Error.Tipo);
        Assert.Equal("La oferta no puede superar el presupuesto de la licitación.", resultado.Error.Mensaje);
    }

    [Fact]
    public async Task Crear_ConMontoIgualAlPresupuesto_EsAceptada()
    {
        var licitacion = SembrarPublicada(1_000_000m);
        var proveedor = SembrarProveedor();

        var resultado = await CrearServicio().CrearAsync(
            new CrearOfertaRequest(licitacion.Id, proveedor.Id, 1_000_000.00m));

        Assert.True(resultado.EsCorrecto);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Crear_ConMontoNoPositivo_EsRechazada(decimal monto)
    {
        var licitacion = SembrarPublicada();
        var proveedor = SembrarProveedor();

        var resultado = await CrearServicio().CrearAsync(
            new CrearOfertaRequest(licitacion.Id, proveedor.Id, monto));

        Assert.False(resultado.EsCorrecto);
        Assert.Equal(CodigosError.MontoInvalido, resultado.Error!.Codigo);
    }

    // ---- HU-020 · Una oferta por proveedor y licitación ----

    [Fact]
    public async Task Crear_UnaSegundaOfertaDelMismoProveedor_EsRechazada()
    {
        var licitacion = SembrarPublicada();
        var proveedor = SembrarProveedor();
        var servicio = CrearServicio();
        await servicio.CrearAsync(new CrearOfertaRequest(licitacion.Id, proveedor.Id, 800_000m));

        var resultado = await servicio.CrearAsync(
            new CrearOfertaRequest(licitacion.Id, proveedor.Id, 700_000m));

        Assert.False(resultado.EsCorrecto);
        Assert.Equal(CodigosError.OfertaDuplicada, resultado.Error!.Codigo);
        Assert.Equal(TipoError.Conflicto, resultado.Error.Tipo);
        Assert.Equal("Este proveedor ya registró una oferta para esta licitación.", resultado.Error.Mensaje);
    }

    [Fact]
    public async Task Crear_ElMismoProveedorEnOtraLicitacion_EsAceptada()
    {
        var primera = SembrarPublicada();
        var segunda = Licitacion.Crear("LIC-101", "Otra", 1_000_000m, Cierre);
        segunda.CambiarEstado(EstadoLicitacion.Publicada);
        _licitaciones.Sembrar(segunda);
        var proveedor = SembrarProveedor();
        var servicio = CrearServicio();
        await servicio.CrearAsync(new CrearOfertaRequest(primera.Id, proveedor.Id, 800_000m));

        var resultado = await servicio.CrearAsync(
            new CrearOfertaRequest(segunda.Id, proveedor.Id, 700_000m));

        Assert.True(resultado.EsCorrecto);
    }

    // ---- HU-018 y HU-021 · Estado y vencimiento ----

    [Fact]
    public async Task Crear_SobreUnaLicitacionEnBorrador_EsRechazada()
    {
        var licitacion = Licitacion.Crear("LIC-102", "Borrador", 1_000_000m, Cierre);
        _licitaciones.Sembrar(licitacion);
        var proveedor = SembrarProveedor();

        var resultado = await CrearServicio().CrearAsync(
            new CrearOfertaRequest(licitacion.Id, proveedor.Id, 800_000m));

        Assert.False(resultado.EsCorrecto);
        Assert.Equal(CodigosError.LicitacionNoPublicada, resultado.Error!.Codigo);
        Assert.Equal("La licitación no está publicada.", resultado.Error.Mensaje);
    }

    [Fact]
    public async Task Crear_JustoAntesDeLaFechaDeCierre_EsAceptada()
    {
        var licitacion = SembrarPublicada();
        var proveedor = SembrarProveedor();
        _reloj.Situar(Cierre.AddSeconds(-1));

        var resultado = await CrearServicio().CrearAsync(
            new CrearOfertaRequest(licitacion.Id, proveedor.Id, 800_000m));

        Assert.True(resultado.EsCorrecto);
    }

    [Fact]
    public async Task Crear_EnElInstanteExactoDelCierre_EsRechazada()
    {
        var licitacion = SembrarPublicada();
        var proveedor = SembrarProveedor();
        _reloj.Situar(Cierre);

        var resultado = await CrearServicio().CrearAsync(
            new CrearOfertaRequest(licitacion.Id, proveedor.Id, 800_000m));

        Assert.False(resultado.EsCorrecto);
        Assert.Equal(CodigosError.LicitacionCerrada, resultado.Error!.Codigo);
        Assert.Equal("La licitación ya cerró; no se admiten más ofertas.", resultado.Error.Mensaje);
    }

    [Fact]
    public async Task Crear_DespuesDeLaFechaDeCierre_EsRechazada()
    {
        var licitacion = SembrarPublicada();
        var proveedor = SembrarProveedor();
        _reloj.Situar(Cierre.AddSeconds(1));

        var resultado = await CrearServicio().CrearAsync(
            new CrearOfertaRequest(licitacion.Id, proveedor.Id, 800_000m));

        Assert.Equal(CodigosError.LicitacionCerrada, resultado.Error!.Codigo);
    }

    [Fact]
    public async Task Crear_SobreUnaLicitacionCerrada_EsRechazada()
    {
        var licitacion = SembrarPublicada();
        licitacion.CambiarEstado(EstadoLicitacion.Cerrada);
        var proveedor = SembrarProveedor();

        var resultado = await CrearServicio().CrearAsync(
            new CrearOfertaRequest(licitacion.Id, proveedor.Id, 800_000m));

        Assert.Equal(CodigosError.LicitacionCerrada, resultado.Error!.Codigo);
    }

    // ---- HU-022 · Inmutabilidad de las ofertas de licitaciones cerradas ----

    [Fact]
    public async Task Actualizar_MientrasLaLicitacionSigueVigente_EsAceptada()
    {
        var (servicio, oferta) = await SembrarOfertaAsync();

        var resultado = await servicio.ActualizarAsync(oferta.Id, new ActualizarOfertaRequest(700_000m));

        Assert.True(resultado.EsCorrecto);
    }

    [Fact]
    public async Task Actualizar_UnaOfertaDeLicitacionVencida_EsRechazada()
    {
        var (servicio, oferta) = await SembrarOfertaAsync();
        _reloj.Situar(Cierre.AddDays(1));

        var resultado = await servicio.ActualizarAsync(oferta.Id, new ActualizarOfertaRequest(700_000m));

        Assert.False(resultado.EsCorrecto);
        Assert.Equal(CodigosError.OfertaInmutable, resultado.Error!.Codigo);
        Assert.Equal("Las ofertas de licitaciones cerradas no pueden modificarse.", resultado.Error.Mensaje);
    }

    [Fact]
    public async Task Actualizar_UnaOfertaDeLicitacionCerrada_EsRechazada()
    {
        var (servicio, oferta) = await SembrarOfertaAsync();
        _licitaciones.Contenido[0].CambiarEstado(EstadoLicitacion.Cerrada);

        var resultado = await servicio.ActualizarAsync(oferta.Id, new ActualizarOfertaRequest(700_000m));

        Assert.Equal(CodigosError.OfertaInmutable, resultado.Error!.Codigo);
    }

    [Fact]
    public async Task Actualizar_ConMontoSuperiorAlPresupuesto_EsRechazada()
    {
        var (servicio, oferta) = await SembrarOfertaAsync();

        var resultado = await servicio.ActualizarAsync(
            oferta.Id,
            new ActualizarOfertaRequest(1_000_000.01m));

        Assert.Equal(CodigosError.OfertaSuperaPresupuesto, resultado.Error!.Codigo);
    }

    [Fact]
    public async Task Eliminar_MientrasLaLicitacionSigueVigente_EsAceptada()
    {
        var (servicio, oferta) = await SembrarOfertaAsync();

        var resultado = await servicio.EliminarAsync(oferta.Id);

        Assert.True(resultado.EsCorrecto);
        Assert.Empty(_ofertas.Contenido);
    }

    [Fact]
    public async Task Eliminar_UnaOfertaDeLicitacionVencida_EsRechazada()
    {
        var (servicio, oferta) = await SembrarOfertaAsync();
        _reloj.Situar(Cierre.AddDays(1));

        var resultado = await servicio.EliminarAsync(oferta.Id);

        Assert.False(resultado.EsCorrecto);
        Assert.Equal(CodigosError.OfertaInmutable, resultado.Error!.Codigo);
        Assert.Single(_ofertas.Contenido);
    }

    [Fact]
    public async Task Eliminar_UnaOfertaDeLicitacionCerrada_EsRechazada()
    {
        var (servicio, oferta) = await SembrarOfertaAsync();
        _licitaciones.Contenido[0].CambiarEstado(EstadoLicitacion.Cerrada);

        var resultado = await servicio.EliminarAsync(oferta.Id);

        Assert.Equal(CodigosError.OfertaInmutable, resultado.Error!.Codigo);
        Assert.Single(_ofertas.Contenido);
    }

    [Fact]
    public async Task Obtener_UnaOfertaInexistente_DevuelveNoEncontrada()
    {
        var resultado = await CrearServicio().ObtenerAsync(Guid.NewGuid());

        Assert.Equal(TipoError.NoEncontrado, resultado.Error!.Tipo);
    }

    [Fact]
    public async Task Listar_FiltraPorLicitacionYPorProveedor()
    {
        var licitacion = SembrarPublicada();
        var proveedor = SembrarProveedor();
        var otroProveedor = SembrarProveedor("Constructora del Valle");
        _ofertas.Sembrar(
            Oferta.Crear(licitacion.Id, proveedor.Id, 800_000m, Ahora),
            Oferta.Crear(licitacion.Id, otroProveedor.Id, 700_000m, Ahora),
            Oferta.Crear(Guid.NewGuid(), proveedor.Id, 500_000m, Ahora));

        var porLicitacion = await CrearServicio().ListarAsync(
            new ConsultaOfertas { LicitacionId = licitacion.Id });
        var porProveedor = await CrearServicio().ListarAsync(
            new ConsultaOfertas { ProveedorId = proveedor.Id });

        Assert.Equal(2, porLicitacion.Valor!.Total);
        Assert.Equal(2, porProveedor.Valor!.Total);
    }

    private async Task<(OfertaService Servicio, OfertaResponse Oferta)> SembrarOfertaAsync()
    {
        var licitacion = SembrarPublicada();
        var proveedor = SembrarProveedor();
        var servicio = CrearServicio();
        var creada = await servicio.CrearAsync(
            new CrearOfertaRequest(licitacion.Id, proveedor.Id, 800_000m));

        return (servicio, creada.Valor!);
    }
}
