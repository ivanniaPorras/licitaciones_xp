using Licitaciones.Domain.Aprobacion;
using Licitaciones.Domain.Dinero;
using Licitaciones.Domain.Licitaciones;
using Licitaciones.Domain.Ofertas;
using Licitaciones.Domain.Proveedores;
using Licitaciones.IntegrationTests.Apoyo;
using Microsoft.EntityFrameworkCore;

namespace Licitaciones.IntegrationTests.Persistencia;

/// <summary>
/// Verifica que las migraciones se apliquen sobre una base vacía y que cada entidad se
/// guarde y se recupere sin perder precisión decimal ni el instante exacto en tiempo
/// universal (HU-010).
/// </summary>
[Collection(PostgresCollection.Nombre)]
public sealed class PersistenciaTests
{
    private readonly PostgresFixture _postgres;

    public PersistenciaTests(PostgresFixture postgres) => _postgres = postgres;

    [Fact]
    public async Task LasMigracionesSeAplicanYNoQuedanPendientes()
    {
        await using var contexto = _postgres.CrearContexto();

        var pendientes = await contexto.Database.GetPendingMigrationsAsync();

        Assert.Empty(pendientes);
    }

    [Fact]
    public async Task Licitacion_SeGuardaYSeRecuperaSinPerderPrecision()
    {
        var fechaCierre = new DateTimeOffset(2026, 9, 30, 17, 0, 0, TimeSpan.FromHours(-6));
        var licitacion = Licitacion.Crear(
            $"LIC-{Guid.NewGuid():N}"[..12],
            "Compra de equipo de cómputo",
            1_250_000.55m,
            fechaCierre);

        await using (var escritura = _postgres.CrearContexto())
        {
            escritura.Licitaciones.Add(licitacion);
            await escritura.SaveChangesAsync();
        }

        await using var lectura = _postgres.CrearContexto();
        var recuperada = await lectura.Licitaciones.SingleAsync(l => l.Id == licitacion.Id);

        Assert.Equal(1_250_000.55m, recuperada.PresupuestoEstimado.Valor);
        Assert.Equal(fechaCierre.ToUniversalTime(), recuperada.FechaCierre.ToUniversalTime());
        Assert.Equal(EstadoLicitacion.Borrador, recuperada.Estado);
    }

    [Fact]
    public async Task Proveedor_SeGuardaConSuNombreOriginalYNormalizado()
    {
        var proveedor = Proveedor.Crear($"  Constructora   {Guid.NewGuid():N}  "[..30]);

        await using (var escritura = _postgres.CrearContexto())
        {
            escritura.Proveedores.Add(proveedor);
            await escritura.SaveChangesAsync();
        }

        await using var lectura = _postgres.CrearContexto();
        var recuperado = await lectura.Proveedores.SingleAsync(p => p.Id == proveedor.Id);

        Assert.Equal(proveedor.Nombre, recuperado.Nombre);
        Assert.Equal(proveedor.NombreNormalizado, recuperado.NombreNormalizado);
    }

    [Fact]
    public async Task Oferta_SeGuardaConSuMontoYFechaExactos()
    {
        var (licitacion, proveedor) = await SembrarLicitacionYProveedorAsync();
        var fechaRegistro = new DateTimeOffset(2026, 9, 1, 8, 30, 15, TimeSpan.FromHours(-6));
        var oferta = Oferta.Crear(licitacion.Id, proveedor.Id, 999_999.99m, fechaRegistro);

        await using (var escritura = _postgres.CrearContexto())
        {
            escritura.Ofertas.Add(oferta);
            await escritura.SaveChangesAsync();
        }

        await using var lectura = _postgres.CrearContexto();
        var recuperada = await lectura.Ofertas.SingleAsync(o => o.Id == oferta.Id);

        Assert.Equal(999_999.99m, recuperada.Monto.Valor);
        Assert.Equal(fechaRegistro.ToUniversalTime(), recuperada.FechaRegistro.ToUniversalTime());
    }

    [Fact]
    public async Task NivelAprobacion_ConservaElRangoAbierto()
    {
        var nivel = NivelAprobacion.Crear(20_000_000.00m, montoMaximoCRC: null, "Junta Directiva");

        await using (var escritura = _postgres.CrearContexto())
        {
            escritura.NivelesAprobacion.Add(nivel);
            await escritura.SaveChangesAsync();
        }

        await using var lectura = _postgres.CrearContexto();
        var recuperado = await lectura.NivelesAprobacion.SingleAsync(n => n.Id == nivel.Id);

        Assert.True(recuperado.EsRangoAbierto);
        Assert.Equal(20_000_000.00m, recuperado.MontoMinimo.Valor);
    }

    [Fact]
    public async Task TipoCambio_ConservaLosCuatroDecimalesDeLaTasa()
    {
        var tipoCambio = TipoCambio.Crear(512.3456m, new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero));

        await using (var escritura = _postgres.CrearContexto())
        {
            escritura.TiposCambio.Add(tipoCambio);
            await escritura.SaveChangesAsync();
        }

        await using var lectura = _postgres.CrearContexto();
        var recuperado = await lectura.TiposCambio.SingleAsync(t => t.Id == tipoCambio.Id);

        Assert.Equal(512.3456m, recuperado.CRCporUSD);
    }

    [Fact]
    public async Task LaSemillaCreaLosTresNivelesDeAprobacion()
    {
        await using var contexto = _postgres.CrearContexto();

        var aprobadores = await contexto.NivelesAprobacion
            .Where(n => n.Aprobador == "Encargado de área"
                     || n.Aprobador == "Gerencia"
                     || n.Aprobador == "Junta Directiva")
            .Select(n => n.Aprobador)
            .ToListAsync();

        Assert.Contains("Encargado de área", aprobadores);
        Assert.Contains("Gerencia", aprobadores);
        Assert.Contains("Junta Directiva", aprobadores);
    }

    [Fact]
    public async Task LaSemillaDejaUnTipoDeCambioActivo()
    {
        await using var contexto = _postgres.CrearContexto();

        var activos = await contexto.TiposCambio.CountAsync(t => t.Activo);

        Assert.Equal(1, activos);
    }

    private async Task<(Licitacion Licitacion, Proveedor Proveedor)> SembrarLicitacionYProveedorAsync()
    {
        var licitacion = Licitacion.Crear(
            $"LIC-{Guid.NewGuid():N}"[..12],
            "Servicios de mantenimiento",
            5_000_000.00m,
            new DateTimeOffset(2026, 12, 31, 17, 0, 0, TimeSpan.Zero));
        var proveedor = Proveedor.Crear($"Proveedor {Guid.NewGuid():N}"[..25]);

        await using var contexto = _postgres.CrearContexto();
        contexto.Licitaciones.Add(licitacion);
        contexto.Proveedores.Add(proveedor);
        await contexto.SaveChangesAsync();

        return (licitacion, proveedor);
    }
}
