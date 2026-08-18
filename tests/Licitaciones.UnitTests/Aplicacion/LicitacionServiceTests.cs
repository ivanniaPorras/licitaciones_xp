using Licitaciones.Application.Comun;
using Licitaciones.Application.Licitaciones;
using Licitaciones.Domain.Licitaciones;
using Licitaciones.UnitTests.Apoyo;
using Licitaciones.UnitTests.Apoyo.Dobles;

namespace Licitaciones.UnitTests.Aplicacion;

/// <summary>
/// Verifica las reglas del servicio de licitaciones: código único, presupuesto válido,
/// transiciones de estado y la prohibición de reducir el presupuesto por debajo de una
/// oferta ya registrada (HU-015, HU-016, HU-017).
/// </summary>
public sealed class LicitacionServiceTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 8, 20, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset CierreFuturo = new(2026, 12, 31, 17, 0, 0, TimeSpan.Zero);

    private readonly RepositorioLicitacionesEnMemoria _licitaciones = new();
    private readonly RepositorioOfertasEnMemoria _ofertas = new();
    private readonly UnidadDeTrabajoFalsa _unidad = new();
    private readonly RelojFalso _reloj = new(Ahora);

    private LicitacionService CrearServicio() => new(_licitaciones, _ofertas, _unidad, _reloj);

    private static CrearLicitacionRequest Peticion(
        string codigo = "LIC-001",
        decimal presupuesto = 1_000_000.00m) =>
        new(codigo, "Compra de equipo de cómputo", presupuesto, CierreFuturo);

    [Fact]
    public async Task Crear_ConDatosValidos_DevuelveLaLicitacionEnBorrador()
    {
        var resultado = await CrearServicio().CrearAsync(Peticion());

        Assert.True(resultado.EsCorrecto);
        Assert.Equal(EstadoLicitacion.Borrador, resultado.Valor!.Estado);
        Assert.Equal(1_000_000.00m, resultado.Valor.PresupuestoEstimadoCRC);
    }

    [Fact]
    public async Task Crear_ConCodigoEquivalenteAUnoExistente_EsRechazado()
    {
        _licitaciones.Sembrar(Licitacion.Crear("LIC-001", "Existente", 500_000m, CierreFuturo));

        var resultado = await CrearServicio().CrearAsync(Peticion(codigo: "  lic-001  "));

        Assert.False(resultado.EsCorrecto);
        Assert.Equal(CodigosError.CodigoLicitacionDuplicado, resultado.Error!.Codigo);
        Assert.Equal("Ya existe una licitación con ese código.", resultado.Error.Mensaje);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Crear_ConPresupuestoNoPositivo_EsRechazado(decimal presupuesto)
    {
        var resultado = await CrearServicio().CrearAsync(Peticion(presupuesto: presupuesto));

        Assert.False(resultado.EsCorrecto);
        Assert.Equal(CodigosError.MontoInvalido, resultado.Error!.Codigo);
        Assert.Equal(TipoError.Validacion, resultado.Error.Tipo);
    }

    [Fact]
    public async Task Publicar_UnaLicitacionEnBorradorConFechaFutura_EsAceptado()
    {
        var licitacion = Licitacion.Crear("LIC-010", "Publicable", 1_000_000m, CierreFuturo);
        _licitaciones.Sembrar(licitacion);

        var resultado = await CrearServicio().CambiarEstadoAsync(
            licitacion.Id,
            new CambiarEstadoRequest(EstadoLicitacion.Publicada));

        Assert.True(resultado.EsCorrecto);
        Assert.Equal(EstadoLicitacion.Publicada, resultado.Valor!.Estado);
    }

    [Fact]
    public async Task Publicar_ConFechaDeCierreYaPasada_EsRechazado()
    {
        var licitacion = Licitacion.Crear("LIC-011", "Vencida", 1_000_000m, Ahora.AddDays(-1));
        _licitaciones.Sembrar(licitacion);

        var resultado = await CrearServicio().CambiarEstadoAsync(
            licitacion.Id,
            new CambiarEstadoRequest(EstadoLicitacion.Publicada));

        Assert.False(resultado.EsCorrecto);
        Assert.Equal(CodigosError.FechaCierreEnElPasado, resultado.Error!.Codigo);
    }

    [Fact]
    public async Task CambiarEstado_DePublicadaABorrador_EsRechazado()
    {
        var licitacion = Licitacion.Crear("LIC-012", "Publicada", 1_000_000m, CierreFuturo);
        licitacion.CambiarEstado(EstadoLicitacion.Publicada);
        _licitaciones.Sembrar(licitacion);

        var resultado = await CrearServicio().CambiarEstadoAsync(
            licitacion.Id,
            new CambiarEstadoRequest(EstadoLicitacion.Borrador));

        Assert.False(resultado.EsCorrecto);
        Assert.Equal(CodigosError.TransicionInvalida, resultado.Error!.Codigo);
        Assert.Equal(TipoError.Conflicto, resultado.Error.Tipo);
        Assert.Equal("Transición de estado no permitida.", resultado.Error.Mensaje);
    }

    [Fact]
    public async Task CambiarEstado_DeCerradaACualquierOtro_EsRechazado()
    {
        var licitacion = Licitacion.Crear("LIC-013", "Cerrada", 1_000_000m, CierreFuturo);
        licitacion.CambiarEstado(EstadoLicitacion.Cerrada);
        _licitaciones.Sembrar(licitacion);

        var resultado = await CrearServicio().CambiarEstadoAsync(
            licitacion.Id,
            new CambiarEstadoRequest(EstadoLicitacion.Publicada));

        Assert.False(resultado.EsCorrecto);
        Assert.Equal(CodigosError.TransicionInvalida, resultado.Error!.Codigo);
    }

    [Fact]
    public async Task Actualizar_ReduciendoElPresupuestoPorDebajoDeUnaOferta_EsRechazado()
    {
        var licitacion = Licitacion.Crear("LIC-020", "Con ofertas", 1_000_000m, CierreFuturo);
        _licitaciones.Sembrar(licitacion);
        _ofertas.Sembrar(Domain.Ofertas.Oferta.Crear(licitacion.Id, Guid.NewGuid(), 800_000m, Ahora));

        var resultado = await CrearServicio().ActualizarAsync(
            licitacion.Id,
            new ActualizarLicitacionRequest("LIC-020", "Con ofertas", 799_999.99m, CierreFuturo));

        Assert.False(resultado.EsCorrecto);
        Assert.Equal(CodigosError.PresupuestoMenorQueOferta, resultado.Error!.Codigo);
        Assert.Contains("800", resultado.Error.Mensaje, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Actualizar_ConElPresupuestoIgualALaOfertaMasAlta_EsAceptado()
    {
        var licitacion = Licitacion.Crear("LIC-021", "Con ofertas", 1_000_000m, CierreFuturo);
        _licitaciones.Sembrar(licitacion);
        _ofertas.Sembrar(Domain.Ofertas.Oferta.Crear(licitacion.Id, Guid.NewGuid(), 800_000m, Ahora));

        var resultado = await CrearServicio().ActualizarAsync(
            licitacion.Id,
            new ActualizarLicitacionRequest("LIC-021", "Con ofertas", 800_000.00m, CierreFuturo));

        Assert.True(resultado.EsCorrecto);
    }

    [Fact]
    public async Task Actualizar_SinOfertas_AdmiteCualquierPresupuestoPositivo()
    {
        var licitacion = Licitacion.Crear("LIC-022", "Sin ofertas", 1_000_000m, CierreFuturo);
        _licitaciones.Sembrar(licitacion);

        var resultado = await CrearServicio().ActualizarAsync(
            licitacion.Id,
            new ActualizarLicitacionRequest("LIC-022", "Sin ofertas", 1.00m, CierreFuturo));

        Assert.True(resultado.EsCorrecto);
    }

    [Fact]
    public async Task Actualizar_ConElCodigoDeOtraLicitacion_EsRechazado()
    {
        var primera = Licitacion.Crear("LIC-030", "Primera", 1_000_000m, CierreFuturo);
        var segunda = Licitacion.Crear("LIC-031", "Segunda", 1_000_000m, CierreFuturo);
        _licitaciones.Sembrar(primera, segunda);

        var resultado = await CrearServicio().ActualizarAsync(
            segunda.Id,
            new ActualizarLicitacionRequest("lic-030", "Segunda", 1_000_000m, CierreFuturo));

        Assert.False(resultado.EsCorrecto);
        Assert.Equal(CodigosError.CodigoLicitacionDuplicado, resultado.Error!.Codigo);
    }

    [Fact]
    public async Task Obtener_UnaLicitacionInexistente_DevuelveNoEncontrado()
    {
        var resultado = await CrearServicio().ObtenerAsync(Guid.NewGuid());

        Assert.False(resultado.EsCorrecto);
        Assert.Equal(TipoError.NoEncontrado, resultado.Error!.Tipo);
    }

    [Fact]
    public async Task Obtener_IndicaSiLaLicitacionYaCerroFuncionalmente()
    {
        var vencida = Licitacion.Crear("LIC-040", "Vencida", 1_000_000m, Ahora.AddHours(-1));
        vencida.CambiarEstado(EstadoLicitacion.Publicada);
        _licitaciones.Sembrar(vencida);

        var resultado = await CrearServicio().ObtenerAsync(vencida.Id);

        // El estado almacenado sigue diciendo Publicada, pero la fecha ya pasó.
        Assert.Equal(EstadoLicitacion.Publicada, resultado.Valor!.Estado);
        Assert.True(resultado.Valor.CerradaFuncionalmente);
    }

    [Fact]
    public async Task Listar_FiltraPorEstado()
    {
        var borrador = Licitacion.Crear("LIC-050", "Borrador", 1_000_000m, CierreFuturo);
        var publicada = Licitacion.Crear("LIC-051", "Publicada", 1_000_000m, CierreFuturo);
        publicada.CambiarEstado(EstadoLicitacion.Publicada);
        _licitaciones.Sembrar(borrador, publicada);

        var resultado = await CrearServicio().ListarAsync(
            new ConsultaLicitaciones { Estado = EstadoLicitacion.Publicada });

        Assert.Single(resultado.Valor!.Elementos);
        Assert.Equal("LIC-051", resultado.Valor.Elementos[0].Codigo);
    }

    [Fact]
    public async Task MejorOferta_SinOfertas_DevuelveLaEtiquetaSinOfertasValidas()
    {
        var licitacion = Licitacion.Crear("LIC-060", "Sin ofertas", 1_000_000m, CierreFuturo);
        _licitaciones.Sembrar(licitacion);

        var resultado = await CrearServicio().ObtenerMejorOfertaAsync(licitacion.Id);

        Assert.True(resultado.EsCorrecto);
        Assert.Null(resultado.Valor!.Oferta);
        Assert.Equal("Sin ofertas válidas", resultado.Valor.Clasificacion);
    }

    [Fact]
    public async Task MejorOferta_DevuelveLaDeMenorMontoConSuAhorroYClasificacion()
    {
        var licitacion = Licitacion.Crear("LIC-061", "Con ofertas", 1_000_000m, CierreFuturo);
        _licitaciones.Sembrar(licitacion);
        _ofertas.Sembrar(
            Domain.Ofertas.Oferta.Crear(licitacion.Id, Guid.NewGuid(), 950_000m, Ahora),
            Domain.Ofertas.Oferta.Crear(licitacion.Id, Guid.NewGuid(), 900_000m, Ahora.AddMinutes(5)));

        var resultado = await CrearServicio().ObtenerMejorOfertaAsync(licitacion.Id);

        Assert.Equal(900_000m, resultado.Valor!.Oferta!.MontoOfertadoCRC);
        Assert.Equal(10m, resultado.Valor.PorcentajeAhorro);
        Assert.Equal("Oferta conveniente", resultado.Valor.Clasificacion);
    }

    [Fact]
    public async Task Eliminar_DaDeBajaLaLicitacion()
    {
        var licitacion = Licitacion.Crear("LIC-070", "Para eliminar", 1_000_000m, CierreFuturo);
        _licitaciones.Sembrar(licitacion);

        var resultado = await CrearServicio().EliminarAsync(licitacion.Id);

        Assert.True(resultado.EsCorrecto);
        Assert.Empty(_licitaciones.Contenido);
    }
}
