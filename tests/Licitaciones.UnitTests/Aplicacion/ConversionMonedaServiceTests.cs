using Licitaciones.Application.Comun;
using Licitaciones.Application.Moneda;
using Licitaciones.Domain.Dinero;
using Licitaciones.UnitTests.Apoyo.Dobles;

namespace Licitaciones.UnitTests.Aplicacion;

/// <summary>
/// Verifica la conversión de colones a dólares contra la tasa vigente y la salvedad de
/// que los colones siguen siendo la única fuente de verdad (HU-027).
/// </summary>
public sealed class ConversionMonedaServiceTests
{
    private static readonly DateTimeOffset Vigencia = new(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly RepositorioTiposCambioEnMemoria _tiposCambio = new();

    private ConversionMonedaService CrearServicio() => new(_tiposCambio);

    private TipoCambio SembrarActivo(decimal tasa)
    {
        var vigente = TipoCambio.Crear(tasa, Vigencia);
        vigente.Activar();
        _tiposCambio.Sembrar(vigente);

        return vigente;
    }

    [Fact]
    public async Task Convertir_DivideElMontoEntreLaTasaVigente()
    {
        SembrarActivo(500.0000m);

        var resultado = await CrearServicio().ConvertirAsync(1_250_000.00m);

        Assert.True(resultado.EsCorrecto);
        Assert.Equal(2_500.00m, resultado.Valor!.MontoUSD);
    }

    [Fact]
    public async Task Convertir_RedondeaADosDecimalesAlejandoseDeCero()
    {
        SembrarActivo(512.0000m);

        // 1 000 / 512 = 1,953125, que redondeado a dos decimales alejándose de cero es 1,95.
        var resultado = await CrearServicio().ConvertirAsync(1_000.00m);

        Assert.Equal(1.95m, resultado.Valor!.MontoUSD);
    }

    [Fact]
    public async Task Convertir_ConMitadExacta_RedondeaHaciaArriba()
    {
        // Con una tasa de 2, un monto de 0,05 colones da 0,025 dólares: la mitad justa
        // entre 0,02 y 0,03. Alejándose de cero sube a 0,03; el redondeo al par que usa
        // Math.Round por omisión bajaría a 0,02.
        SembrarActivo(2.0000m);

        var resultado = await CrearServicio().ConvertirAsync(0.05m);

        Assert.Equal(0.03m, resultado.Valor!.MontoUSD);
    }

    [Fact]
    public async Task Convertir_DevuelveLaTasaUsadaYSuFechaDeVigencia()
    {
        SembrarActivo(512.0000m);

        var resultado = await CrearServicio().ConvertirAsync(1_250_000.00m);

        // La tasa y su fecha viajan con el monto: la vista debe poder mostrarlas al lado.
        Assert.Equal(512.0000m, resultado.Valor!.CRCporUSD);
        Assert.Equal(Vigencia, resultado.Valor.FechaVigencia);
    }

    [Fact]
    public async Task Convertir_NoModificaElMontoEnColones()
    {
        SembrarActivo(512.0000m);

        var resultado = await CrearServicio().ConvertirAsync(1_250_000.00m);

        // Los colones son la fuente de verdad: la conversión es una representación añadida.
        Assert.Equal(1_250_000.00m, resultado.Valor!.MontoCRC);
    }

    [Fact]
    public async Task Convertir_SinTasaActiva_DevuelveMensajeControlado()
    {
        _tiposCambio.Sembrar(TipoCambio.Crear(512.0000m, Vigencia));

        var resultado = await CrearServicio().ConvertirAsync(1_250_000.00m);

        Assert.False(resultado.EsCorrecto);
        Assert.Equal(CodigosError.SinTipoCambioActivo, resultado.Error!.Codigo);
        Assert.Equal(TipoError.Conflicto, resultado.Error.Tipo);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Convertir_ConMontoNoPositivo_EsRechazado(decimal monto)
    {
        SembrarActivo(512.0000m);

        var resultado = await CrearServicio().ConvertirAsync(monto);

        Assert.False(resultado.EsCorrecto);
        Assert.Equal(CodigosError.MontoInvalido, resultado.Error!.Codigo);
    }

    [Fact]
    public async Task Convertir_ConMasDeDosDecimales_EsRechazado()
    {
        SembrarActivo(512.0000m);

        var resultado = await CrearServicio().ConvertirAsync(1_000.001m);

        Assert.False(resultado.EsCorrecto);
        Assert.Equal(CodigosError.MontoInvalido, resultado.Error!.Codigo);
    }
}
