using Licitaciones.Domain.Licitaciones;
using Licitaciones.UnitTests.Apoyo;

namespace Licitaciones.UnitTests.Dominio;

/// <summary>
/// Verifica que una licitación deje de admitir ofertas al alcanzarse su fecha de cierre,
/// aunque su estado almacenado siga indicando Publicada (HU-003).
/// </summary>
public sealed class CierreFuncionalLicitacionTests
{
    private static readonly DateTimeOffset FechaCierre =
        new(2026, 8, 20, 17, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Publicada_AntesDeLaFechaDeCierre_NoEstaCerrada()
    {
        var licitacion = LicitacionPublicada();
        var reloj = new RelojFalso(FechaCierre.AddSeconds(-1));

        Assert.False(licitacion.EstaCerradaFuncionalmente(reloj));
    }

    [Fact]
    public void Publicada_EnElInstanteExactoDelCierre_YaEstaCerrada()
    {
        var licitacion = LicitacionPublicada();
        var reloj = new RelojFalso(FechaCierre);

        Assert.True(licitacion.EstaCerradaFuncionalmente(reloj));
    }

    [Fact]
    public void Publicada_DespuesDeLaFechaDeCierre_EstaCerrada()
    {
        var licitacion = LicitacionPublicada();
        var reloj = new RelojFalso(FechaCierre.AddSeconds(1));

        Assert.True(licitacion.EstaCerradaFuncionalmente(reloj));
    }

    [Fact]
    public void Cerrada_AunConFechaFutura_EstaCerrada()
    {
        var licitacion = LicitacionPublicada();
        licitacion.CambiarEstado(EstadoLicitacion.Cerrada);
        var reloj = new RelojFalso(FechaCierre.AddDays(-10));

        Assert.True(licitacion.EstaCerradaFuncionalmente(reloj));
    }

    [Fact]
    public void Borrador_ConFechaFutura_NoEstaCerrada()
    {
        var licitacion = LicitacionBorrador();
        var reloj = new RelojFalso(FechaCierre.AddDays(-1));

        Assert.False(licitacion.EstaCerradaFuncionalmente(reloj));
    }

    [Fact]
    public void LaComparacionSeHaceEnUtc()
    {
        // La misma marca temporal expresada en otro huso horario debe producir el mismo
        // resultado: el desfase no puede alterar la decisión.
        var licitacion = LicitacionPublicada();
        var mismoInstanteEnCostaRica = FechaCierre.ToOffset(TimeSpan.FromHours(-6));

        Assert.True(licitacion.EstaCerradaFuncionalmente(new RelojFalso(mismoInstanteEnCostaRica)));
    }

    private static Licitacion LicitacionBorrador() =>
        Licitacion.Crear("LIC-001", "Compra de equipo de cómputo", 1_000_000.00m, FechaCierre);

    private static Licitacion LicitacionPublicada()
    {
        var licitacion = LicitacionBorrador();
        licitacion.CambiarEstado(EstadoLicitacion.Publicada);
        return licitacion;
    }
}
