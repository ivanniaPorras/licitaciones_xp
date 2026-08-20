using Licitaciones.Domain.Dinero;

namespace Licitaciones.UnitTests.Dominio;

/// <summary>
/// Verifica las invariantes del tipo de cambio: tasa positiva y control de cuál está
/// activo.
/// </summary>
public sealed class TipoCambioTests
{
    private static readonly DateTimeOffset Vigencia = new(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Crear_GuardaLaTasaYSuFechaDeVigencia()
    {
        var tipoCambio = TipoCambio.Crear(512.00m, Vigencia);

        Assert.Equal(512.00m, tipoCambio.CRCporUSD);
        Assert.Equal(Vigencia, tipoCambio.FechaVigencia);
    }

    [Fact]
    public void Crear_NaceInactivo()
    {
        var tipoCambio = TipoCambio.Crear(512.00m, Vigencia);

        Assert.False(tipoCambio.Activo);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-512.50)]
    public void Crear_ConTasaCeroONegativa_EsRechazado(decimal tasa)
    {
        Assert.Throws<MontoInvalidoException>(() => TipoCambio.Crear(tasa, Vigencia));
    }

    [Fact]
    public void Crear_AdmiteTasasConCuatroDecimales()
    {
        // La tasa admite más precisión que un monto porque es un factor de conversión,
        // no una cantidad de dinero.
        var tipoCambio = TipoCambio.Crear(512.3456m, Vigencia);

        Assert.Equal(512.3456m, tipoCambio.CRCporUSD);
    }

    [Fact]
    public void CambiarTasa_ActualizaLaTasaYSuFechaSinTocarElUso()
    {
        var tipoCambio = TipoCambio.Crear(512.00m, Vigencia);
        tipoCambio.Activar();
        var nuevaVigencia = Vigencia.AddMonths(1);

        tipoCambio.CambiarTasa(530.2500m, nuevaVigencia);

        Assert.Equal(530.2500m, tipoCambio.CRCporUSD);
        Assert.Equal(nuevaVigencia, tipoCambio.FechaVigencia);
        Assert.True(tipoCambio.Activo);
    }

    [Fact]
    public void CambiarTasa_ConTasaNoPositiva_DejaElRegistroIntacto()
    {
        var tipoCambio = TipoCambio.Crear(512.00m, Vigencia);

        Assert.Throws<MontoInvalidoException>(() => tipoCambio.CambiarTasa(0m, Vigencia));
        Assert.Equal(512.00m, tipoCambio.CRCporUSD);
    }

    [Fact]
    public void Activar_MarcaElRegistroComoActivo()
    {
        var tipoCambio = TipoCambio.Crear(512.00m, Vigencia);

        tipoCambio.Activar();

        Assert.True(tipoCambio.Activo);
    }

    [Fact]
    public void Desactivar_QuitaLaMarcaDeActivo()
    {
        var tipoCambio = TipoCambio.Crear(512.00m, Vigencia);
        tipoCambio.Activar();

        tipoCambio.Desactivar();

        Assert.False(tipoCambio.Activo);
    }
}
