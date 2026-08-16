using Licitaciones.Domain.Proveedores;

namespace Licitaciones.UnitTests.Dominio;

/// <summary>
/// Verifica que dos escrituras distintas del mismo proveedor se reconozcan como iguales
/// tras normalizar espacios, mayúsculas y forma Unicode (HU-005).
/// </summary>
public sealed class NormalizadorNombreProveedorTests
{
    [Theory]
    [InlineData("Empresa Central")]
    [InlineData("empresa central")]
    [InlineData("EMPRESA CENTRAL")]
    [InlineData("  Empresa   Central  ")]
    [InlineData("Empresa\tCentral")]
    [InlineData("Empresa\u00A0Central")]
    public void Normalizar_VariantesDelMismoNombre_ProduceElMismoResultado(string nombre)
    {
        Assert.Equal("empresa central", NormalizadorNombreProveedor.Normalizar(nombre));
    }

    [Fact]
    public void Normalizar_ColapsaEspaciosRepetidosInteriores()
    {
        Assert.Equal(
            "constructora del valle",
            NormalizadorNombreProveedor.Normalizar("Constructora    del     Valle"));
    }

    [Fact]
    public void Normalizar_UnificaLasFormasUnicodeDeUnaPalabraAcentuada()
    {
        // La misma palabra con caracteres precompuestos y con letra base más marca
        // combinante debe producir el mismo resultado. Se escriben con escapes para que
        // la diferencia sea visible al leer la prueba.
        const string precompuesta = "Compañía Nacional";
        const string descompuesta = "Compan\u0303i\u0301a Nacional";

        Assert.Equal(
            NormalizadorNombreProveedor.Normalizar(precompuesta),
            NormalizadorNombreProveedor.Normalizar(descompuesta));
    }

    [Fact]
    public void Normalizar_NombresDistintos_ProduceResultadosDistintos()
    {
        Assert.NotEqual(
            NormalizadorNombreProveedor.Normalizar("Empresa Central"),
            NormalizadorNombreProveedor.Normalizar("Empresa Central S.A."));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalizar_NombreVacio_EsRechazado(string nombre)
    {
        Assert.Throws<ArgumentException>(() => NormalizadorNombreProveedor.Normalizar(nombre));
    }

    [Fact]
    public void Normalizar_NombreNulo_EsRechazado()
    {
        Assert.Throws<ArgumentNullException>(() => NormalizadorNombreProveedor.Normalizar(null!));
    }
}
