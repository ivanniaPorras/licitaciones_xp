using Licitaciones.Domain.Ofertas;

namespace Licitaciones.UnitTests.Dominio;

/// <summary>
/// Verifica que la mejor oferta sea la de menor monto y que los empates se resuelvan por
/// orden de llegada, de forma determinista (HU-008).
/// </summary>
public sealed class EvaluadorMejorOfertaTests
{
    private static readonly Guid Licitacion = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTimeOffset Manana = new(2026, 8, 18, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void SinOfertas_NoDevuelveNinguna()
    {
        Assert.Null(EvaluadorMejorOferta.Seleccionar([]));
    }

    [Fact]
    public void ConUnaSolaOferta_DevuelveEsa()
    {
        var unica = Oferta(800_000m, Manana);

        Assert.Same(unica, EvaluadorMejorOferta.Seleccionar([unica]));
    }

    [Fact]
    public void DevuelveLaDeMenorMonto()
    {
        var cara = Oferta(900_000m, Manana);
        var barata = Oferta(750_000m, Manana.AddHours(3));
        var intermedia = Oferta(820_000m, Manana.AddHours(1));

        Assert.Same(barata, EvaluadorMejorOferta.Seleccionar([cara, barata, intermedia]));
    }

    [Fact]
    public void EnEmpateDeMonto_DevuelveLaRegistradaPrimero()
    {
        var primera = Oferta(800_000m, Manana);
        var segunda = Oferta(800_000m, Manana.AddMinutes(1));

        Assert.Same(primera, EvaluadorMejorOferta.Seleccionar([segunda, primera]));
    }

    [Fact]
    public void ElOrdenDeEntradaNoAlteraElResultado()
    {
        var primera = Oferta(800_000m, Manana);
        var segunda = Oferta(800_000m, Manana.AddMinutes(1));

        Assert.Same(primera, EvaluadorMejorOferta.Seleccionar([primera, segunda]));
        Assert.Same(primera, EvaluadorMejorOferta.Seleccionar([segunda, primera]));
    }

    [Fact]
    public void EnEmpateDeMontoYFecha_ElDesempateEsDeterminista()
    {
        var una = Oferta(800_000m, Manana);
        var otra = Oferta(800_000m, Manana);

        var ganadora = EvaluadorMejorOferta.Seleccionar([una, otra]);
        var ganadoraConOrdenInverso = EvaluadorMejorOferta.Seleccionar([otra, una]);

        Assert.Same(ganadora, ganadoraConOrdenInverso);
    }

    [Fact]
    public void LaComparacionDeFechasSeHaceEnUtc()
    {
        // El mismo instante expresado en el huso de Costa Rica llega después que una
        // oferta registrada una hora antes en UTC.
        var temprana = Oferta(800_000m, Manana);
        var tardiaEnOtroHuso = Oferta(800_000m, Manana.AddHours(1).ToOffset(TimeSpan.FromHours(-6)));

        Assert.Same(temprana, EvaluadorMejorOferta.Seleccionar([tardiaEnOtroHuso, temprana]));
    }

    private static Oferta Oferta(decimal monto, DateTimeOffset fechaRegistro) =>
        Domain.Ofertas.Oferta.Crear(Licitacion, Guid.NewGuid(), monto, fechaRegistro);
}
