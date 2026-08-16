using Licitaciones.Domain.Licitaciones;
using Licitaciones.Domain.Ofertas;
using Licitaciones.Domain.Proveedores;
using Licitaciones.Infrastructure.Persistencia.Errores;
using Licitaciones.IntegrationTests.Apoyo;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Licitaciones.IntegrationTests.Persistencia;

/// <summary>
/// Verifica que la base de datos haga cumplir por sí sola las reglas de unicidad,
/// integridad referencial y montos positivos, aunque una condición de carrera burle la
/// comprobación previa del servidor.
/// </summary>
[Collection(PostgresCollection.Nombre)]
public sealed class RestriccionesTests
{
    private readonly PostgresFixture _postgres;

    public RestriccionesTests(PostgresFixture postgres) => _postgres = postgres;

    [Fact]
    public async Task DosLicitacionesConElMismoCodigoNormalizado_SonRechazadas()
    {
        var codigo = $"LIC-{Guid.NewGuid():N}"[..12];

        await using (var contexto = _postgres.CrearContexto())
        {
            contexto.Licitaciones.Add(NuevaLicitacion(codigo));
            await contexto.SaveChangesAsync();
        }

        await using var segundo = _postgres.CrearContexto();
        // Misma cadena con otra caja y espacios: normaliza al mismo código.
        segundo.Licitaciones.Add(NuevaLicitacion($"  {codigo.ToLowerInvariant()}  "));

        var error = await Assert.ThrowsAsync<DbUpdateException>(() => segundo.SaveChangesAsync());

        Assert.Equal(EstadosSqlPostgres.ViolacionUnicidad, EstadoSql(error));
    }

    [Fact]
    public async Task DosProveedoresConElMismoNombreNormalizado_SonRechazados()
    {
        var nombre = $"Empresa {Guid.NewGuid():N}"[..24];

        await using (var contexto = _postgres.CrearContexto())
        {
            contexto.Proveedores.Add(Proveedor.Crear(nombre));
            await contexto.SaveChangesAsync();
        }

        await using var segundo = _postgres.CrearContexto();
        segundo.Proveedores.Add(Proveedor.Crear($"  {nombre.ToUpperInvariant()}   "));

        var error = await Assert.ThrowsAsync<DbUpdateException>(() => segundo.SaveChangesAsync());

        Assert.Equal(EstadosSqlPostgres.ViolacionUnicidad, EstadoSql(error));
    }

    [Fact]
    public async Task DosOfertasDelMismoProveedorParaLaMismaLicitacion_SonRechazadas()
    {
        var (licitacion, proveedor) = await SembrarAsync();
        var fecha = new DateTimeOffset(2026, 9, 1, 9, 0, 0, TimeSpan.Zero);

        await using (var contexto = _postgres.CrearContexto())
        {
            contexto.Ofertas.Add(Oferta.Crear(licitacion.Id, proveedor.Id, 900_000.00m, fecha));
            await contexto.SaveChangesAsync();
        }

        await using var segundo = _postgres.CrearContexto();
        segundo.Ofertas.Add(Oferta.Crear(licitacion.Id, proveedor.Id, 800_000.00m, fecha.AddHours(1)));

        var error = await Assert.ThrowsAsync<DbUpdateException>(() => segundo.SaveChangesAsync());

        Assert.Equal(EstadosSqlPostgres.ViolacionUnicidad, EstadoSql(error));
    }

    [Fact]
    public async Task UnaOfertaSinLicitacion_EsRechazada()
    {
        var (_, proveedor) = await SembrarAsync();

        await using var contexto = _postgres.CrearContexto();
        contexto.Ofertas.Add(Oferta.Crear(
            Guid.NewGuid(),
            proveedor.Id,
            500_000.00m,
            new DateTimeOffset(2026, 9, 1, 9, 0, 0, TimeSpan.Zero)));

        var error = await Assert.ThrowsAsync<DbUpdateException>(() => contexto.SaveChangesAsync());

        Assert.Equal(EstadosSqlPostgres.ViolacionIntegridadReferencial, EstadoSql(error));
    }

    [Fact]
    public async Task UnMontoNoPositivoEsRechazadoPorLaBaseAunqueSeBurleElDominio()
    {
        // Se escribe por SQL directo para saltarse las validaciones del dominio y
        // comprobar que la restricción de verificación existe realmente en la tabla.
        var (licitacion, proveedor) = await SembrarAsync();

        await using var conexion = new NpgsqlConnection(_postgres.CadenaConexion);
        await conexion.OpenAsync();
        await using var comando = conexion.CreateCommand();
        comando.CommandText = """
            INSERT INTO ofertas (id, licitacion_id, proveedor_id, monto_ofertado_crc,
                                 fecha_registro, created_at, updated_at)
            VALUES (@id, @licitacion, @proveedor, 0, now(), now(), now())
            """;
        comando.Parameters.AddWithValue("id", Guid.NewGuid());
        comando.Parameters.AddWithValue("licitacion", licitacion.Id);
        comando.Parameters.AddWithValue("proveedor", proveedor.Id);

        var error = await Assert.ThrowsAsync<PostgresException>(() => comando.ExecuteNonQueryAsync());

        Assert.Equal(EstadosSqlPostgres.ViolacionRestriccionVerificacion, error.SqlState);
        Assert.Equal("ck_ofertas_monto_positivo", error.ConstraintName);
    }

    [Fact]
    public async Task NoPuedeHaberDosTiposDeCambioActivos()
    {
        // La semilla ya dejó uno activo; intentar un segundo debe fallar contra el índice
        // único parcial.
        await using var contexto = _postgres.CrearContexto();
        var otro = Domain.Dinero.TipoCambio.Crear(
            600.0000m,
            new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero));
        otro.Activar();
        contexto.TiposCambio.Add(otro);

        var error = await Assert.ThrowsAsync<DbUpdateException>(() => contexto.SaveChangesAsync());

        Assert.Equal(EstadosSqlPostgres.ViolacionUnicidad, EstadoSql(error));
    }

    private static string? EstadoSql(DbUpdateException error) =>
        (error.InnerException as PostgresException)?.SqlState;

    private static Licitacion NuevaLicitacion(string codigo) =>
        Licitacion.Crear(
            codigo,
            "Licitación de prueba",
            1_000_000.00m,
            new DateTimeOffset(2026, 12, 31, 17, 0, 0, TimeSpan.Zero));

    private async Task<(Licitacion Licitacion, Proveedor Proveedor)> SembrarAsync()
    {
        var licitacion = NuevaLicitacion($"LIC-{Guid.NewGuid():N}"[..12]);
        var proveedor = Proveedor.Crear($"Proveedor {Guid.NewGuid():N}"[..25]);

        await using var contexto = _postgres.CrearContexto();
        contexto.Licitaciones.Add(licitacion);
        contexto.Proveedores.Add(proveedor);
        await contexto.SaveChangesAsync();

        return (licitacion, proveedor);
    }
}
