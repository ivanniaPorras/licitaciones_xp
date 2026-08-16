using Licitaciones.Domain.Proveedores;

namespace Licitaciones.UnitTests.Dominio;

/// <summary>
/// Verifica qué caracteres admite el nombre de un proveedor: letras, números, espacios,
/// punto, coma y paréntesis, y ningún otro símbolo (HU-006).
/// </summary>
public sealed class ValidadorNombreProveedorTests
{
    [Theory]
    [InlineData("Empresa Central S.A.")]
    [InlineData("Constructora (CR), Ltda.")]
    [InlineData("Proveedor 2000")]
    [InlineData("Compañía Nacional de Suministros")]
    [InlineData("Ferretería El Álamo")]
    [InlineData("Servicios Integrales, S.R.L. (Zona Norte)")]
    public void EsValido_NombreConCaracteresPermitidos_EsAceptado(string nombre)
    {
        Assert.True(ValidadorNombreProveedor.EsValido(nombre));
    }

    [Theory]
    [InlineData("Empresa@Central")]
    [InlineData("Empresa & Cía")]
    [InlineData("Empresa/Central")]
    [InlineData("Empresa#1")]
    [InlineData("Empresa_Central")]
    [InlineData("Empresa-Central")]
    [InlineData("Empresa\"Central\"")]
    [InlineData("Empresa[CR]")]
    public void EsValido_NombreConSimboloNoPermitido_EsRechazado(string nombre)
    {
        Assert.False(ValidadorNombreProveedor.EsValido(nombre));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EsValido_NombreVacioOSoloEspacios_EsRechazado(string nombre)
    {
        Assert.False(ValidadorNombreProveedor.EsValido(nombre));
    }

    [Fact]
    public void EsValido_NombreNulo_EsRechazado()
    {
        Assert.False(ValidadorNombreProveedor.EsValido(null));
    }

    [Fact]
    public void Validar_NombreInvalido_LanzaConElMensajeAcordado()
    {
        var error = Assert.Throws<NombreProveedorInvalidoException>(
            () => ValidadorNombreProveedor.Validar("Empresa@Central"));

        Assert.Equal(
            "El nombre solo admite letras, números, espacios, punto, coma y paréntesis.",
            error.Message);
    }

    [Fact]
    public void Validar_NombreValido_NoLanza()
    {
        ValidadorNombreProveedor.Validar("Constructora (CR), Ltda.");
    }
}
