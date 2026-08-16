using Licitaciones.Domain.Aprobacion;
using Licitaciones.Domain.Dinero;
using Licitaciones.Domain.Proveedores;

namespace Licitaciones.UnitTests.Dominio;

/// <summary>
/// Verifica las invariantes que cada entidad protege al crearse: nombre normalizado y
/// válido en el proveedor, rango coherente en el nivel de aprobación y tasa positiva en
/// el tipo de cambio.
/// </summary>
public sealed class EntidadesDominioTests
{
    [Fact]
    public void Proveedor_GuardaElNombreOriginalYSuFormaNormalizada()
    {
        var proveedor = Proveedor.Crear("  Empresa   Central  ");

        Assert.Equal("  Empresa   Central  ", proveedor.Nombre);
        Assert.Equal("empresa central", proveedor.NombreNormalizado);
    }

    [Fact]
    public void Proveedor_ConCaracterNoPermitido_EsRechazado()
    {
        Assert.Throws<NombreProveedorInvalidoException>(() => Proveedor.Crear("Empresa@Central"));
    }

    [Fact]
    public void Proveedor_ConNombreVacio_EsRechazado()
    {
        Assert.Throws<NombreProveedorInvalidoException>(() => Proveedor.Crear("   "));
    }

    [Fact]
    public void NivelAprobacion_ConRangoCerrado_GuardaSusLimites()
    {
        var nivel = NivelAprobacion.Crear(1_000_000.00m, 9_999_999.99m, "Gerencia");

        Assert.Equal(1_000_000.00m, nivel.MontoMinimo.Valor);
        Assert.Equal(9_999_999.99m, nivel.MontoMaximo!.Value.Valor);
        Assert.False(nivel.EsRangoAbierto);
    }

    [Fact]
    public void NivelAprobacion_SinMontoMaximo_EsRangoAbierto()
    {
        var nivel = NivelAprobacion.Crear(10_000_000.00m, montoMaximoCRC: null, "Junta Directiva");

        Assert.True(nivel.EsRangoAbierto);
        Assert.Null(nivel.MontoMaximo);
    }

    [Fact]
    public void NivelAprobacion_ConMaximoMenorQueElMinimo_EsRechazado()
    {
        Assert.Throws<RangoAprobacionInvalidoException>(
            () => NivelAprobacion.Crear(1_000_000.00m, 999_999.99m, "Gerencia"));
    }

    [Fact]
    public void NivelAprobacion_ConMaximoIgualAlMinimo_EsAceptado()
    {
        var nivel = NivelAprobacion.Crear(500.00m, 500.00m, "Encargado de área");

        Assert.Equal(500.00m, nivel.MontoMaximo!.Value.Valor);
    }

    [Fact]
    public void NivelAprobacion_ConMinimoCero_EsRechazado()
    {
        Assert.Throws<MontoInvalidoException>(
            () => NivelAprobacion.Crear(0m, 1_000m, "Encargado de área"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void NivelAprobacion_SinAprobador_EsRechazado(string aprobador)
    {
        Assert.Throws<RangoAprobacionInvalidoException>(
            () => NivelAprobacion.Crear(1_000m, 2_000m, aprobador));
    }

    [Theory]
    [InlineData(1_000_000.00, 1_000_000.00, true)]
    [InlineData(1_000_000.00, 999_999.99, false)]
    [InlineData(1_000_000.00, 9_999_999.99, true)]
    [InlineData(1_000_000.00, 10_000_000.00, false)]
    public void NivelAprobacion_IndicaSiUnMontoCaeEnSuRango(
        decimal minimo, decimal monto, bool esperado)
    {
        var nivel = NivelAprobacion.Crear(minimo, 9_999_999.99m, "Gerencia");

        Assert.Equal(esperado, nivel.Cubre(MontoCRC.Crear(monto)));
    }

    [Fact]
    public void NivelAprobacion_AbiertoCubreCualquierMontoDesdeSuMinimo()
    {
        var nivel = NivelAprobacion.Crear(10_000_000.00m, montoMaximoCRC: null, "Junta Directiva");

        Assert.True(nivel.Cubre(MontoCRC.Crear(50_000_000.00m)));
        Assert.False(nivel.Cubre(MontoCRC.Crear(9_999_999.99m)));
    }
}
