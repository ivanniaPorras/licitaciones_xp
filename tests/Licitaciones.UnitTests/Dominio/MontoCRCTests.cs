using Licitaciones.Domain.Dinero;

namespace Licitaciones.UnitTests.Dominio;

/// <summary>
/// Verifica que ningún monto pueda ser cero ni negativo y que la precisión de dos
/// decimales se conserve exactamente (HU-007).
/// </summary>
public sealed class MontoCRCTests
{
    [Theory]
    [InlineData(0.01)]
    [InlineData(1)]
    [InlineData(1_000_000)]
    [InlineData(999_999_999_999.99)]
    public void Crear_MontoPositivo_EsAceptado(decimal valor)
    {
        var monto = MontoCRC.Crear(valor);

        Assert.Equal(valor, monto.Valor);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(-1)]
    [InlineData(-1_000_000)]
    public void Crear_MontoCeroONegativo_EsRechazado(decimal valor)
    {
        Assert.Throws<MontoInvalidoException>(() => MontoCRC.Crear(valor));
    }

    [Fact]
    public void Crear_MontoCero_UsaElMensajeAcordado()
    {
        var error = Assert.Throws<MontoInvalidoException>(() => MontoCRC.Crear(0m));

        Assert.Equal("El monto debe ser mayor que cero.", error.Message);
    }

    [Theory]
    [InlineData(0.005)]
    [InlineData(1.001)]
    [InlineData(1234.5678)]
    public void Crear_MontoConMasDeDosDecimales_EsRechazado(decimal valor)
    {
        Assert.Throws<MontoInvalidoException>(() => MontoCRC.Crear(valor));
    }

    [Fact]
    public void Crear_ConservaLaPrecisionExacta()
    {
        var monto = MontoCRC.Crear(1_250_000.55m);

        Assert.Equal(1_250_000.55m, monto.Valor);
    }

    [Fact]
    public void DosMontosConElMismoValor_SonIguales()
    {
        Assert.Equal(MontoCRC.Crear(500.00m), MontoCRC.Crear(500.00m));
    }

    [Fact]
    public void DosMontosConValorDistinto_NoSonIguales()
    {
        Assert.NotEqual(MontoCRC.Crear(500.00m), MontoCRC.Crear(500.01m));
    }

    [Fact]
    public void SeComparanPorSuValor()
    {
        Assert.True(MontoCRC.Crear(999.99m) < MontoCRC.Crear(1_000.00m));
    }
}
