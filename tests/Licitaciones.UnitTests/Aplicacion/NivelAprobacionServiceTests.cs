using Licitaciones.Application.Aprobacion;
using Licitaciones.Application.Comun;
using Licitaciones.Domain.Aprobacion;
using Licitaciones.UnitTests.Apoyo.Dobles;

namespace Licitaciones.UnitTests.Aplicacion;

/// <summary>
/// Verifica las reglas del servicio de niveles de aprobación: rangos sin traslape, un
/// único rango abierto y resolución del aprobador consultando la tabla
/// (HU-024, HU-025).
/// </summary>
public sealed class NivelAprobacionServiceTests
{
    private readonly RepositorioNivelesAprobacionEnMemoria _niveles = new();
    private readonly UnidadDeTrabajoFalsa _unidad = new();

    private NivelAprobacionService CrearServicio() => new(_niveles, _unidad);

    private void SembrarSemilla() => _niveles.Sembrar(
        NivelAprobacion.Crear(0.01m, 999_999.99m, "Encargado de área"),
        NivelAprobacion.Crear(1_000_000.00m, 9_999_999.99m, "Gerencia"),
        NivelAprobacion.Crear(10_000_000.00m, null, "Junta Directiva"));

    // ---- HU-025 · Resolución del aprobador ----

    [Theory]
    [InlineData(0.01, "Encargado de área")]
    [InlineData(500_000.00, "Encargado de área")]
    [InlineData(999_999.99, "Encargado de área")]
    [InlineData(1_000_000.00, "Gerencia")]
    [InlineData(5_000_000.00, "Gerencia")]
    [InlineData(9_999_999.99, "Gerencia")]
    [InlineData(10_000_000.00, "Junta Directiva")]
    [InlineData(50_000_000.00, "Junta Directiva")]
    public async Task ObtenerAprobador_DevuelveElNivelDeLaTabla(decimal monto, string esperado)
    {
        SembrarSemilla();

        var resultado = await CrearServicio().ObtenerAprobadorAsync(monto);

        Assert.True(resultado.EsCorrecto);
        Assert.Equal(esperado, resultado.Valor!.Aprobador);
    }

    [Fact]
    public async Task ObtenerAprobador_SinNingunNivelAplicable_DevuelveMensajeControlado()
    {
        // Sin rangos cargados no hay ninguno que cubra el monto.
        var resultado = await CrearServicio().ObtenerAprobadorAsync(500_000m);

        Assert.False(resultado.EsCorrecto);
        Assert.Equal(CodigosError.SinNivelAplicable, resultado.Error!.Codigo);
    }

    [Fact]
    public async Task ObtenerAprobador_ConMontoNoPositivo_EsRechazado()
    {
        SembrarSemilla();

        var resultado = await CrearServicio().ObtenerAprobadorAsync(0m);

        Assert.False(resultado.EsCorrecto);
        Assert.Equal(CodigosError.MontoInvalido, resultado.Error!.Codigo);
    }

    // ---- HU-024 · No traslape ----

    [Fact]
    public async Task Crear_UnRangoQueNoSeTraslapa_EsAceptado()
    {
        _niveles.Sembrar(NivelAprobacion.Crear(0.01m, 999_999.99m, "Encargado de área"));

        var resultado = await CrearServicio().CrearAsync(
            new CrearNivelAprobacionRequest(1_000_000.00m, 9_999_999.99m, "Gerencia"));

        Assert.True(resultado.EsCorrecto);
    }

    [Theory]
    [InlineData(500_000.00, 2_000_000.00)]
    [InlineData(0.01, 999_999.99)]
    [InlineData(100.00, 200.00)]
    [InlineData(999_999.99, 5_000_000.00)]
    public async Task Crear_UnRangoQueSeTraslapa_EsRechazado(decimal minimo, decimal maximo)
    {
        _niveles.Sembrar(NivelAprobacion.Crear(0.01m, 999_999.99m, "Encargado de área"));

        var resultado = await CrearServicio().CrearAsync(
            new CrearNivelAprobacionRequest(minimo, maximo, "Otro aprobador"));

        Assert.False(resultado.EsCorrecto);
        Assert.Equal(CodigosError.RangoTraslapado, resultado.Error!.Codigo);
        Assert.Equal("El rango se traslapa con un nivel existente.", resultado.Error.Mensaje);
    }

    [Fact]
    public async Task Crear_UnRangoAbiertoQueAlcanzaAOtroExistente_EsRechazado()
    {
        _niveles.Sembrar(NivelAprobacion.Crear(10_000_000.00m, 20_000_000.00m, "Gerencia"));

        var resultado = await CrearServicio().CrearAsync(
            new CrearNivelAprobacionRequest(5_000_000.00m, null, "Junta Directiva"));

        Assert.Equal(CodigosError.RangoTraslapado, resultado.Error!.Codigo);
    }

    // ---- HU-024 · Un solo rango abierto ----

    [Fact]
    public async Task Crear_UnSegundoRangoAbierto_EsRechazado()
    {
        _niveles.Sembrar(NivelAprobacion.Crear(10_000_000.00m, null, "Junta Directiva"));

        var resultado = await CrearServicio().CrearAsync(
            new CrearNivelAprobacionRequest(50_000_000.00m, null, "Asamblea"));

        Assert.False(resultado.EsCorrecto);
        Assert.Equal(CodigosError.RangoAbiertoDuplicado, resultado.Error!.Codigo);
        Assert.Equal("Ya existe un nivel sin monto máximo.", resultado.Error.Mensaje);
    }

    [Fact]
    public async Task Crear_ElPrimerRangoAbierto_EsAceptado()
    {
        _niveles.Sembrar(NivelAprobacion.Crear(0.01m, 999_999.99m, "Encargado de área"));

        var resultado = await CrearServicio().CrearAsync(
            new CrearNivelAprobacionRequest(1_000_000.00m, null, "Junta Directiva"));

        Assert.True(resultado.EsCorrecto);
    }

    // ---- Validaciones del rango ----

    [Fact]
    public async Task Crear_ConMaximoMenorQueElMinimo_EsRechazado()
    {
        var resultado = await CrearServicio().CrearAsync(
            new CrearNivelAprobacionRequest(1_000_000.00m, 999_999.99m, "Gerencia"));

        Assert.False(resultado.EsCorrecto);
        Assert.Equal(CodigosError.RangoInvalido, resultado.Error!.Codigo);
    }

    [Fact]
    public async Task Crear_ConMinimoCero_EsRechazado()
    {
        var resultado = await CrearServicio().CrearAsync(
            new CrearNivelAprobacionRequest(0m, 1_000m, "Encargado de área"));

        Assert.False(resultado.EsCorrecto);
        Assert.Equal(CodigosError.MontoInvalido, resultado.Error!.Codigo);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Crear_SinAprobador_EsRechazado(string aprobador)
    {
        var resultado = await CrearServicio().CrearAsync(
            new CrearNivelAprobacionRequest(1_000m, 2_000m, aprobador));

        Assert.False(resultado.EsCorrecto);
        Assert.Equal(CodigosError.RangoInvalido, resultado.Error!.Codigo);
    }

    // ---- Edición ----

    [Fact]
    public async Task Actualizar_UnNivelSinCambiarSuRango_EsAceptado()
    {
        SembrarSemilla();
        var gerencia = _niveles.Contenido[1];

        var resultado = await CrearServicio().ActualizarAsync(
            gerencia.Id,
            new ActualizarNivelAprobacionRequest(1_000_000.00m, 9_999_999.99m, "Gerencia General"));

        // No debe compararse consigo mismo al comprobar el traslape.
        Assert.True(resultado.EsCorrecto);
        Assert.Equal("Gerencia General", resultado.Valor!.Aprobador);
    }

    [Fact]
    public async Task Actualizar_InvadiendoElRangoDeOtro_EsRechazado()
    {
        SembrarSemilla();
        var gerencia = _niveles.Contenido[1];

        var resultado = await CrearServicio().ActualizarAsync(
            gerencia.Id,
            new ActualizarNivelAprobacionRequest(500_000.00m, 9_999_999.99m, "Gerencia"));

        Assert.False(resultado.EsCorrecto);
        Assert.Equal(CodigosError.RangoTraslapado, resultado.Error!.Codigo);
    }

    [Fact]
    public async Task Actualizar_UnNivelInexistente_DevuelveNoEncontrado()
    {
        var resultado = await CrearServicio().ActualizarAsync(
            Guid.NewGuid(),
            new ActualizarNivelAprobacionRequest(1_000m, 2_000m, "Alguien"));

        Assert.Equal(TipoError.NoEncontrado, resultado.Error!.Tipo);
    }

    [Fact]
    public async Task Eliminar_QuitaElNivel()
    {
        SembrarSemilla();
        var gerencia = _niveles.Contenido[1];

        var resultado = await CrearServicio().EliminarAsync(gerencia.Id);

        Assert.True(resultado.EsCorrecto);
        Assert.Equal(2, _niveles.Contenido.Count);
    }

    // ---- HU-030 · Paginación, búsqueda y ordenamiento ----

    [Fact]
    public async Task Listar_DevuelveLosNivelesOrdenadosPorMontoMinimo()
    {
        SembrarSemilla();

        var resultado = await CrearServicio().ListarAsync(new ConsultaNivelesAprobacion());

        Assert.Equal(3, resultado.Valor!.Elementos.Count);
        Assert.Equal("Encargado de área", resultado.Valor.Elementos[0].Aprobador);
        Assert.Equal("Junta Directiva", resultado.Valor.Elementos[2].Aprobador);
    }

    [Fact]
    public async Task Listar_InformaElTotalAunqueLaPaginaTraigaMenos()
    {
        SembrarSemilla();

        var resultado = await CrearServicio().ListarAsync(
            new ConsultaNivelesAprobacion { Tamano = 2 });

        Assert.Equal(2, resultado.Valor!.Elementos.Count);
        Assert.Equal(3, resultado.Valor.Total);
        Assert.Equal(2, resultado.Valor.TotalPaginas);
    }

    [Fact]
    public async Task Listar_LaSegundaPaginaTraeElResto()
    {
        SembrarSemilla();

        var resultado = await CrearServicio().ListarAsync(
            new ConsultaNivelesAprobacion { Pagina = 2, Tamano = 2 });

        Assert.Single(resultado.Valor!.Elementos);
        Assert.Equal("Junta Directiva", resultado.Valor.Elementos[0].Aprobador);
    }

    [Fact]
    public async Task Listar_ConBusqueda_FiltraPorAprobador()
    {
        SembrarSemilla();

        var resultado = await CrearServicio().ListarAsync(
            new ConsultaNivelesAprobacion { Busqueda = "gerencia" });

        Assert.Single(resultado.Valor!.Elementos);
        Assert.Equal("Gerencia", resultado.Valor.Elementos[0].Aprobador);
    }

    [Fact]
    public async Task Listar_ConBusquedaSinCoincidencias_DevuelveLaPaginaVacia()
    {
        SembrarSemilla();

        var resultado = await CrearServicio().ListarAsync(
            new ConsultaNivelesAprobacion { Busqueda = "nadie" });

        Assert.Empty(resultado.Valor!.Elementos);
        Assert.Equal(0, resultado.Valor.Total);
    }

    [Fact]
    public async Task Listar_ConOrdenDescendente_InvierteElListado()
    {
        SembrarSemilla();

        var resultado = await CrearServicio().ListarAsync(
            new ConsultaNivelesAprobacion { Orden = "montoMinimo:desc" });

        Assert.Equal("Junta Directiva", resultado.Valor!.Elementos[0].Aprobador);
        Assert.Equal("Encargado de área", resultado.Valor.Elementos[2].Aprobador);
    }

    [Fact]
    public async Task Listar_OrdenandoPorAprobador_UsaElOrdenAlfabetico()
    {
        SembrarSemilla();

        var resultado = await CrearServicio().ListarAsync(
            new ConsultaNivelesAprobacion { Orden = "aprobador:asc" });

        Assert.Equal("Encargado de área", resultado.Valor!.Elementos[0].Aprobador);
        Assert.Equal("Gerencia", resultado.Valor.Elementos[1].Aprobador);
        Assert.Equal("Junta Directiva", resultado.Valor.Elementos[2].Aprobador);
    }
}
