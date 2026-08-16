using Licitaciones.Domain.Licitaciones;
using Licitaciones.Domain.Ofertas;
using Licitaciones.Domain.Proveedores;
using Licitaciones.IntegrationTests.Apoyo;
using Microsoft.EntityFrameworkCore;

namespace Licitaciones.IntegrationTests.Persistencia;

/// <summary>
/// Verifica que las fechas de auditoría se asignen solas y que dar de baja una licitación
/// o un proveedor conserve la fila y sus ofertas (HU-011).
/// </summary>
[Collection(PostgresCollection.Nombre)]
public sealed class AuditoriaYBorradoLogicoTests
{
    private readonly PostgresFixture _postgres;

    public AuditoriaYBorradoLogicoTests(PostgresFixture postgres) => _postgres = postgres;

    [Fact]
    public async Task AlCrear_SeAsignanLasFechasDeCreacionYModificacion()
    {
        var momento = new DateTimeOffset(2026, 8, 20, 10, 0, 0, TimeSpan.Zero);
        var reloj = new RelojFijo(momento);
        var proveedor = Proveedor.Crear($"Auditoria {Guid.NewGuid():N}"[..24]);

        await using (var contexto = _postgres.CrearContexto(reloj))
        {
            contexto.Proveedores.Add(proveedor);
            await contexto.SaveChangesAsync();
        }

        await using var lectura = _postgres.CrearContexto();
        var recuperado = await lectura.Proveedores.SingleAsync(p => p.Id == proveedor.Id);

        Assert.Equal(momento, recuperado.CreatedAt);
        Assert.Equal(momento, recuperado.UpdatedAt);
        Assert.Null(recuperado.DeletedAt);
    }

    [Fact]
    public async Task AlModificar_SoloCambiaLaFechaDeModificacion()
    {
        var creacion = new DateTimeOffset(2026, 8, 20, 10, 0, 0, TimeSpan.Zero);
        var proveedor = Proveedor.Crear($"Modificado {Guid.NewGuid():N}"[..24]);

        await using (var contexto = _postgres.CrearContexto(new RelojFijo(creacion)))
        {
            contexto.Proveedores.Add(proveedor);
            await contexto.SaveChangesAsync();
        }

        var modificacion = creacion.AddDays(3);
        await using (var contexto = _postgres.CrearContexto(new RelojFijo(modificacion)))
        {
            var seguido = await contexto.Proveedores.SingleAsync(p => p.Id == proveedor.Id);
            seguido.Renombrar($"Renombrado {Guid.NewGuid():N}"[..24]);
            await contexto.SaveChangesAsync();
        }

        await using var lectura = _postgres.CrearContexto();
        var recuperado = await lectura.Proveedores.SingleAsync(p => p.Id == proveedor.Id);

        Assert.Equal(creacion, recuperado.CreatedAt);
        Assert.Equal(modificacion, recuperado.UpdatedAt);
    }

    [Fact]
    public async Task AlEliminarUnProveedor_LaFilaSeConservaConSuFechaDeBaja()
    {
        var baja = new DateTimeOffset(2026, 9, 5, 16, 0, 0, TimeSpan.Zero);
        var proveedor = Proveedor.Crear($"Dado de baja {Guid.NewGuid():N}"[..26]);

        await using (var contexto = _postgres.CrearContexto())
        {
            contexto.Proveedores.Add(proveedor);
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = _postgres.CrearContexto(new RelojFijo(baja)))
        {
            var seguido = await contexto.Proveedores.SingleAsync(p => p.Id == proveedor.Id);
            contexto.Proveedores.Remove(seguido);
            await contexto.SaveChangesAsync();
        }

        await using var lectura = _postgres.CrearContexto();

        // El filtro global lo excluye de las consultas ordinarias...
        Assert.False(await lectura.Proveedores.AnyAsync(p => p.Id == proveedor.Id));

        // ...pero la fila sigue en la tabla con su fecha de baja.
        var conBaja = await lectura.Proveedores
            .IgnoreQueryFilters()
            .SingleAsync(p => p.Id == proveedor.Id);
        Assert.Equal(baja, conBaja.DeletedAt);
    }

    [Fact]
    public async Task AlDarDeBajaUnaLicitacion_SusOfertasSeConservan()
    {
        var licitacion = Licitacion.Crear(
            $"LIC-{Guid.NewGuid():N}"[..12],
            "Licitación con ofertas",
            2_000_000.00m,
            new DateTimeOffset(2026, 12, 31, 17, 0, 0, TimeSpan.Zero));
        var proveedor = Proveedor.Crear($"Oferente {Guid.NewGuid():N}"[..24]);
        var oferta = Oferta.Crear(
            licitacion.Id,
            proveedor.Id,
            1_500_000.00m,
            new DateTimeOffset(2026, 9, 1, 9, 0, 0, TimeSpan.Zero));

        await using (var contexto = _postgres.CrearContexto())
        {
            contexto.Licitaciones.Add(licitacion);
            contexto.Proveedores.Add(proveedor);
            contexto.Ofertas.Add(oferta);
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = _postgres.CrearContexto())
        {
            var seguida = await contexto.Licitaciones.SingleAsync(l => l.Id == licitacion.Id);
            contexto.Licitaciones.Remove(seguida);
            await contexto.SaveChangesAsync();
        }

        await using var lectura = _postgres.CrearContexto();

        Assert.False(await lectura.Licitaciones.AnyAsync(l => l.Id == licitacion.Id));
        Assert.True(await lectura.Ofertas.AnyAsync(o => o.Id == oferta.Id));
    }
}
