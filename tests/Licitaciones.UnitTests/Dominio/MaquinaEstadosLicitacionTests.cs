using Licitaciones.Domain.Licitaciones;

namespace Licitaciones.UnitTests.Dominio;

/// <summary>
/// Verifica las cinco transiciones de la tabla del ciclo de estados de una licitación,
/// incluidas las que deben rechazarse (HU-002).
/// </summary>
public sealed class MaquinaEstadosLicitacionTests
{
    [Theory]
    [InlineData(EstadoLicitacion.Borrador, EstadoLicitacion.Publicada)]
    [InlineData(EstadoLicitacion.Borrador, EstadoLicitacion.Cerrada)]
    [InlineData(EstadoLicitacion.Publicada, EstadoLicitacion.Cerrada)]
    public void Transicion_Permitida_EsAceptada(EstadoLicitacion origen, EstadoLicitacion destino)
    {
        Assert.True(MaquinaEstadosLicitacion.EsTransicionPermitida(origen, destino));
    }

    [Theory]
    [InlineData(EstadoLicitacion.Publicada, EstadoLicitacion.Borrador)]
    [InlineData(EstadoLicitacion.Cerrada, EstadoLicitacion.Publicada)]
    [InlineData(EstadoLicitacion.Cerrada, EstadoLicitacion.Borrador)]
    public void Transicion_Prohibida_EsRechazada(EstadoLicitacion origen, EstadoLicitacion destino)
    {
        Assert.False(MaquinaEstadosLicitacion.EsTransicionPermitida(origen, destino));
    }

    [Theory]
    [InlineData(EstadoLicitacion.Borrador)]
    [InlineData(EstadoLicitacion.Publicada)]
    [InlineData(EstadoLicitacion.Cerrada)]
    public void Transicion_AlMismoEstado_EsRechazada(EstadoLicitacion estado)
    {
        Assert.False(MaquinaEstadosLicitacion.EsTransicionPermitida(estado, estado));
    }

    [Fact]
    public void Cerrada_NoAdmiteNingunaTransicion()
    {
        var destinos = MaquinaEstadosLicitacion.TransicionesDesde(EstadoLicitacion.Cerrada);

        Assert.Empty(destinos);
    }

    [Fact]
    public void Validar_TransicionProhibida_LanzaExcepcionConMensajeControlado()
    {
        var error = Assert.Throws<TransicionEstadoInvalidaException>(
            () => MaquinaEstadosLicitacion.Validar(EstadoLicitacion.Publicada, EstadoLicitacion.Borrador));

        Assert.Equal("Transición de estado no permitida.", error.Message);
    }

    [Fact]
    public void Validar_TransicionPermitida_NoLanza()
    {
        MaquinaEstadosLicitacion.Validar(EstadoLicitacion.Borrador, EstadoLicitacion.Publicada);
    }
}
