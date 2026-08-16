using Licitaciones.Domain.Licitaciones;

namespace Licitaciones.UnitTests.Dominio;

/// <summary>
/// Verifica que el código de una licitación se compare ignorando espacios laterales y
/// diferencias entre mayúsculas y minúsculas (HU-004).
/// </summary>
public sealed class NormalizadorCodigoTests
{
    [Theory]
    [InlineData("LIC-001")]
    [InlineData("lic-001")]
    [InlineData("Lic-001")]
    [InlineData("  LIC-001  ")]
    [InlineData("\tlic-001\n")]
    public void Normalizar_VariantesDelMismoCodigo_ProduceElMismoResultado(string codigo)
    {
        Assert.Equal("LIC-001", NormalizadorCodigo.Normalizar(codigo));
    }

    [Fact]
    public void Normalizar_CodigosDistintos_ProduceResultadosDistintos()
    {
        Assert.NotEqual(
            NormalizadorCodigo.Normalizar("LIC-001"),
            NormalizadorCodigo.Normalizar("LIC-002"));
    }

    [Fact]
    public void Normalizar_NoColapsaEspaciosInteriores()
    {
        // A diferencia del nombre de proveedor, el código conserva su forma interna:
        // solo se recortan los extremos y se pasa a mayúsculas.
        Assert.Equal("LIC 001", NormalizadorCodigo.Normalizar(" lic 001 "));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalizar_CodigoVacio_EsRechazado(string codigo)
    {
        Assert.Throws<ArgumentException>(() => NormalizadorCodigo.Normalizar(codigo));
    }

    [Fact]
    public void Normalizar_CodigoNulo_EsRechazado()
    {
        Assert.Throws<ArgumentNullException>(() => NormalizadorCodigo.Normalizar(null!));
    }
}
