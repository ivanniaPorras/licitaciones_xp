using Licitaciones.Domain.Dinero;
using Licitaciones.Domain.Proveedores;
using Licitaciones.IntegrationTests.Apoyo;
using Microsoft.EntityFrameworkCore;

namespace Licitaciones.IntegrationTests.Persistencia;

/// <summary>
/// Verifica la concurrencia optimista sobre <c>xmin</c> y que una transacción fallida no
/// deje el tipo de cambio en un estado inconsistente (HU-012).
/// </summary>
[Collection(PostgresCollection.Nombre)]
public sealed class ConcurrenciaYTransaccionesTests
{
    private readonly PostgresFixture _postgres;

    public ConcurrenciaYTransaccionesTests(PostgresFixture postgres) => _postgres = postgres;

    [Fact]
    public async Task DosEdicionesSimultaneas_LaSegundaEnGuardarEsRechazada()
    {
        var proveedor = Proveedor.Crear($"Concurrente {Guid.NewGuid():N}"[..26]);

        await using (var contexto = _postgres.CrearContexto())
        {
            contexto.Proveedores.Add(proveedor);
            await contexto.SaveChangesAsync();
        }

        // Dos personas abren el mismo registro al mismo tiempo.
        await using var primera = _postgres.CrearContexto();
        await using var segunda = _postgres.CrearContexto();
        var vistaPorLaPrimera = await primera.Proveedores.SingleAsync(p => p.Id == proveedor.Id);
        var vistaPorLaSegunda = await segunda.Proveedores.SingleAsync(p => p.Id == proveedor.Id);

        vistaPorLaPrimera.Renombrar($"Guardado primero {Guid.NewGuid():N}"[..30]);
        await primera.SaveChangesAsync();

        vistaPorLaSegunda.Renombrar($"Guardado despues {Guid.NewGuid():N}"[..30]);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => segunda.SaveChangesAsync());
    }

    [Fact]
    public async Task UnaEdicionSobreDatosFrescos_SeGuardaSinConflicto()
    {
        var proveedor = Proveedor.Crear($"Sin conflicto {Guid.NewGuid():N}"[..28]);

        await using (var contexto = _postgres.CrearContexto())
        {
            contexto.Proveedores.Add(proveedor);
            await contexto.SaveChangesAsync();
        }

        await using var contexto2 = _postgres.CrearContexto();
        var seguido = await contexto2.Proveedores.SingleAsync(p => p.Id == proveedor.Id);
        var nombreNuevo = $"Renombrado ok {Guid.NewGuid():N}"[..28];
        seguido.Renombrar(nombreNuevo);
        await contexto2.SaveChangesAsync();

        await using var lectura = _postgres.CrearContexto();
        var recuperado = await lectura.Proveedores.SingleAsync(p => p.Id == proveedor.Id);
        Assert.Equal(nombreNuevo, recuperado.Nombre);
    }

    [Fact]
    public async Task SiFallaLaActivacionDeUnTipoDeCambio_NoQuedaNingunoInconsistente()
    {
        // Se desactiva el vigente y se intenta activar otro dentro de la misma
        // transacción, provocando un fallo a mitad. Al revertir, debe seguir habiendo
        // exactamente un tipo de cambio activo.
        await using var contexto = _postgres.CrearContexto();
        await using var transaccion = await contexto.Database.BeginTransactionAsync();

        var vigente = await contexto.TiposCambio.SingleAsync(t => t.Activo);
        vigente.Desactivar();
        await contexto.SaveChangesAsync();

        var invalido = TipoCambio.Crear(700.0000m, new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero));
        invalido.Activar();
        contexto.TiposCambio.Add(invalido);
        contexto.TiposCambio.Add(TipoCambio.Crear(
            800.0000m,
            new DateTimeOffset(2026, 11, 2, 0, 0, 0, TimeSpan.Zero)));

        // Se fuerza el fallo activando un segundo registro: el índice único parcial lo
        // impide y la transacción completa debe revertirse.
        var otroActivo = TipoCambio.Crear(900.0000m, new DateTimeOffset(2026, 11, 3, 0, 0, 0, TimeSpan.Zero));
        otroActivo.Activar();
        contexto.TiposCambio.Add(otroActivo);

        await Assert.ThrowsAsync<DbUpdateException>(() => contexto.SaveChangesAsync());
        await transaccion.RollbackAsync();

        await using var lectura = _postgres.CrearContexto();
        Assert.Equal(1, await lectura.TiposCambio.CountAsync(t => t.Activo));
    }
}
