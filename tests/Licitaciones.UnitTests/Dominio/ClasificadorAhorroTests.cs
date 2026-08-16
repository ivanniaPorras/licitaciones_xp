using Licitaciones.Domain.Dinero;
using Licitaciones.Domain.Ofertas;

namespace Licitaciones.UnitTests.Dominio;

/// <summary>
/// Verifica el cálculo del ahorro y las cuatro etiquetas de clasificación, con especial
/// atención a la frontera del 10 % (HU-009).
/// </summary>
public sealed class ClasificadorAhorroTests
{
    private static readonly MontoCRC Presupuesto = MontoCRC.Crear(1_000_000.00m);

    [Fact]
    public void SinOfertas_DevuelveSinOfertasValidas()
    {
        var resultado = ClasificadorAhorro.Clasificar(Presupuesto, mejorOferta: null);

        Assert.Equal("Sin ofertas válidas", resultado.Etiqueta);
        Assert.Null(resultado.PorcentajeAhorro);
    }

    [Fact]
    public void OfertaIgualAlPresupuesto_DevuelveOfertaValidaSinAhorro()
    {
        var resultado = ClasificadorAhorro.Clasificar(Presupuesto, MontoCRC.Crear(1_000_000.00m));

        Assert.Equal("Oferta válida sin ahorro", resultado.Etiqueta);
        Assert.Equal(0m, resultado.PorcentajeAhorro);
    }

    [Fact]
    public void AhorroDeExactamenteDiezPorCiento_DevuelveOfertaConveniente()
    {
        var resultado = ClasificadorAhorro.Clasificar(Presupuesto, MontoCRC.Crear(900_000.00m));

        Assert.Equal("Oferta conveniente", resultado.Etiqueta);
        Assert.Equal(10m, resultado.PorcentajeAhorro);
    }

    [Fact]
    public void AhorroSuperiorADiezPorCiento_DevuelveOfertaConveniente()
    {
        var resultado = ClasificadorAhorro.Clasificar(Presupuesto, MontoCRC.Crear(500_000.00m));

        Assert.Equal("Oferta conveniente", resultado.Etiqueta);
        Assert.Equal(50m, resultado.PorcentajeAhorro);
    }

    [Fact]
    public void AhorroDeNuevePuntoNoventaYNuevePorCiento_DevuelveOfertaAceptable()
    {
        var resultado = ClasificadorAhorro.Clasificar(Presupuesto, MontoCRC.Crear(900_100.00m));

        Assert.Equal("Oferta aceptable", resultado.Etiqueta);
        Assert.Equal(9.99m, resultado.PorcentajeAhorro);
    }

    [Fact]
    public void AhorroMinimo_DevuelveOfertaAceptable()
    {
        var resultado = ClasificadorAhorro.Clasificar(Presupuesto, MontoCRC.Crear(999_999.99m));

        Assert.Equal("Oferta aceptable", resultado.Etiqueta);
    }

    [Theory]
    [InlineData(1_000_000, 900_000, 10)]
    [InlineData(1_000_000, 750_000, 25)]
    [InlineData(2_000_000, 1_500_000, 25)]
    [InlineData(800_000, 800_000, 0)]
    public void CalculaElPorcentajeDeAhorro(decimal presupuesto, decimal oferta, decimal esperado)
    {
        var resultado = ClasificadorAhorro.Clasificar(
            MontoCRC.Crear(presupuesto),
            MontoCRC.Crear(oferta));

        Assert.Equal(esperado, resultado.PorcentajeAhorro);
    }

    [Fact]
    public void ElPorcentajeSeRedondeaADosDecimales()
    {
        // 1 000 000 - 666 666,67 = 333 333,33 → 33,333333 % → 33,33 %
        var resultado = ClasificadorAhorro.Clasificar(Presupuesto, MontoCRC.Crear(666_666.67m));

        Assert.Equal(33.33m, resultado.PorcentajeAhorro);
    }
}
