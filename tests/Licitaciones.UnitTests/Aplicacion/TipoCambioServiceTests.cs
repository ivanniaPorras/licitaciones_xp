using Licitaciones.Application.Comun;
using Licitaciones.Application.Moneda;
using Licitaciones.Domain.Dinero;
using Licitaciones.UnitTests.Apoyo.Dobles;

namespace Licitaciones.UnitTests.Aplicacion;

/// <summary>
/// Verifica las reglas del servicio de tipos de cambio: tasa positiva, un único registro
/// activo y activación atómica (HU-026).
/// </summary>
public sealed class TipoCambioServiceTests
{
    private static readonly DateTimeOffset Vigencia = new(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly RepositorioTiposCambioEnMemoria _tiposCambio = new();
    private readonly UnidadDeTrabajoFalsa _unidad = new();

    private TipoCambioService CrearServicio() => new(_tiposCambio, _unidad);

    private TipoCambio SembrarActivo(decimal tasa = 512.0000m)
    {
        var vigente = TipoCambio.Crear(tasa, Vigencia);
        vigente.Activar();
        _tiposCambio.Sembrar(vigente);

        return vigente;
    }

    // ---- Registro de tasas ----

    [Fact]
    public async Task Crear_ConTasaPositiva_LaGuardaInactiva()
    {
        var resultado = await CrearServicio().CrearAsync(
            new CrearTipoCambioRequest(525.5000m, Vigencia));

        Assert.True(resultado.EsCorrecto);
        Assert.Equal(525.5000m, resultado.Valor!.CRCporUSD);

        // Registrar una tasa no la pone en uso: activarla es una decisión aparte.
        Assert.False(resultado.Valor.Activo);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-512.50)]
    public async Task Crear_ConTasaCeroONegativa_EsRechazado(decimal tasa)
    {
        var resultado = await CrearServicio().CrearAsync(new CrearTipoCambioRequest(tasa, Vigencia));

        Assert.False(resultado.EsCorrecto);
        Assert.Equal(CodigosError.TasaInvalida, resultado.Error!.Codigo);
        Assert.Equal(TipoError.Validacion, resultado.Error.Tipo);
    }

    [Fact]
    public async Task Crear_ConMasDeCuatroDecimales_EsRechazado()
    {
        var resultado = await CrearServicio().CrearAsync(
            new CrearTipoCambioRequest(512.123456m, Vigencia));

        Assert.False(resultado.EsCorrecto);
        Assert.Equal(CodigosError.TasaInvalida, resultado.Error!.Codigo);
    }

    [Fact]
    public async Task Actualizar_CambiaLaTasaYSuFechaDeVigencia()
    {
        var vigente = SembrarActivo();
        var nuevaFecha = Vigencia.AddMonths(1);

        var resultado = await CrearServicio().ActualizarAsync(
            vigente.Id,
            new ActualizarTipoCambioRequest(530.0000m, nuevaFecha));

        Assert.True(resultado.EsCorrecto);
        Assert.Equal(530.0000m, resultado.Valor!.CRCporUSD);
        Assert.Equal(nuevaFecha, resultado.Valor.FechaVigencia);

        // Editar una tasa no le quita el uso que ya tenía.
        Assert.True(resultado.Valor.Activo);
    }

    [Fact]
    public async Task Actualizar_ConTasaNoPositiva_EsRechazado()
    {
        var vigente = SembrarActivo();

        var resultado = await CrearServicio().ActualizarAsync(
            vigente.Id,
            new ActualizarTipoCambioRequest(0m, Vigencia));

        Assert.False(resultado.EsCorrecto);
        Assert.Equal(CodigosError.TasaInvalida, resultado.Error!.Codigo);

        // La tasa rechazada no debe haber tocado el registro guardado.
        Assert.Equal(512.0000m, vigente.CRCporUSD);
    }

    [Fact]
    public async Task Actualizar_UnRegistroInexistente_DevuelveNoEncontrado()
    {
        var resultado = await CrearServicio().ActualizarAsync(
            Guid.NewGuid(),
            new ActualizarTipoCambioRequest(530.0000m, Vigencia));

        Assert.False(resultado.EsCorrecto);
        Assert.Equal(CodigosError.TipoCambioNoEncontrado, resultado.Error!.Codigo);
        Assert.Equal(TipoError.NoEncontrado, resultado.Error.Tipo);
    }

    // ---- Un único registro activo ----

    [Fact]
    public async Task Activar_DejaInactivoAlQueEstabaVigente()
    {
        var anterior = SembrarActivo();
        var nuevo = TipoCambio.Crear(530.0000m, Vigencia.AddMonths(1));
        _tiposCambio.Sembrar(nuevo);

        var resultado = await CrearServicio().ActivarAsync(nuevo.Id);

        Assert.True(resultado.EsCorrecto);
        Assert.True(nuevo.Activo);
        Assert.False(anterior.Activo);
    }

    [Fact]
    public async Task Activar_NuncaDejaMasDeUnRegistroActivo()
    {
        SembrarActivo();
        var segundo = TipoCambio.Crear(530.0000m, Vigencia.AddMonths(1));
        var tercero = TipoCambio.Crear(540.0000m, Vigencia.AddMonths(2));
        _tiposCambio.Sembrar(segundo, tercero);

        var servicio = CrearServicio();
        await servicio.ActivarAsync(segundo.Id);
        await servicio.ActivarAsync(tercero.Id);

        Assert.Single(_tiposCambio.Contenido.Where(t => t.Activo));
        Assert.True(tercero.Activo);
    }

    [Fact]
    public async Task Activar_ElRegistroQueYaEstabaVigente_LoDejaActivo()
    {
        var vigente = SembrarActivo();

        var resultado = await CrearServicio().ActivarAsync(vigente.Id);

        Assert.True(resultado.EsCorrecto);
        Assert.True(vigente.Activo);
        Assert.Single(_tiposCambio.Contenido.Where(t => t.Activo));
    }

    [Fact]
    public async Task Activar_UnRegistroInexistente_DevuelveNoEncontradoYNoTocaElVigente()
    {
        var vigente = SembrarActivo();

        var resultado = await CrearServicio().ActivarAsync(Guid.NewGuid());

        Assert.False(resultado.EsCorrecto);
        Assert.Equal(CodigosError.TipoCambioNoEncontrado, resultado.Error!.Codigo);

        // La operación va en transacción: si el destino no existe, el vigente no cambia.
        Assert.True(vigente.Activo);
    }

    // ---- Consultas y eliminación ----

    [Fact]
    public async Task Listar_DevuelveLasTasasDeLaMasRecienteALaMasAntigua()
    {
        SembrarActivo();
        _tiposCambio.Sembrar(TipoCambio.Crear(530.0000m, Vigencia.AddMonths(1)));

        var resultado = await CrearServicio().ListarAsync();

        Assert.True(resultado.EsCorrecto);
        Assert.Equal(530.0000m, resultado.Valor![0].CRCporUSD);
        Assert.Equal(512.0000m, resultado.Valor[1].CRCporUSD);
    }

    [Fact]
    public async Task ObtenerVigente_DevuelveLaTasaActiva()
    {
        SembrarActivo();
        _tiposCambio.Sembrar(TipoCambio.Crear(530.0000m, Vigencia.AddMonths(1)));

        var resultado = await CrearServicio().ObtenerVigenteAsync();

        Assert.True(resultado.EsCorrecto);
        Assert.Equal(512.0000m, resultado.Valor!.CRCporUSD);
    }

    [Fact]
    public async Task ObtenerVigente_SinNingunaTasaActiva_DevuelveMensajeControlado()
    {
        _tiposCambio.Sembrar(TipoCambio.Crear(512.0000m, Vigencia));

        var resultado = await CrearServicio().ObtenerVigenteAsync();

        Assert.False(resultado.EsCorrecto);
        Assert.Equal(CodigosError.SinTipoCambioActivo, resultado.Error!.Codigo);
        Assert.Equal(TipoError.Conflicto, resultado.Error.Tipo);
    }

    [Fact]
    public async Task Eliminar_QuitaLaTasaYConfirmaLosCambios()
    {
        var vigente = SembrarActivo();

        var resultado = await CrearServicio().EliminarAsync(vigente.Id);

        Assert.True(resultado.EsCorrecto);
        Assert.Empty(_tiposCambio.Contenido);
        Assert.Equal(1, _unidad.Confirmaciones);
    }

    [Fact]
    public async Task Eliminar_UnRegistroInexistente_DevuelveNoEncontrado()
    {
        var resultado = await CrearServicio().EliminarAsync(Guid.NewGuid());

        Assert.False(resultado.EsCorrecto);
        Assert.Equal(CodigosError.TipoCambioNoEncontrado, resultado.Error!.Codigo);
    }
}
