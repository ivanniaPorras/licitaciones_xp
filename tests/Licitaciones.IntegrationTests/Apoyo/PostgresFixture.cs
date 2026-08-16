using Licitaciones.Domain.Tiempo;
using Licitaciones.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Licitaciones.IntegrationTests.Apoyo;

/// <summary>
/// Levanta un contenedor con PostgreSQL 16 real y le aplica las migraciones del proyecto.
/// Las pruebas de integración se ejecutan contra este motor y nunca contra una base en
/// memoria: buena parte de lo que verifican —índices únicos parciales, restricciones de
/// verificación, tipos numéricos exactos y concurrencia por <c>xmin</c>— no existe fuera
/// de PostgreSQL.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _contenedor = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("licitaciones_test")
        .WithUsername("licitaciones_test")
        .WithPassword("licitaciones_test")
        .Build();

    /// <summary>Reloj compartido que las pruebas pueden mover para fijar las fechas de auditoría.</summary>
    public RelojFijo Reloj { get; } = new(new DateTimeOffset(2026, 8, 15, 12, 0, 0, TimeSpan.Zero));

    /// <summary>Cadena de conexión al contenedor en ejecución.</summary>
    public string CadenaConexion => _contenedor.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _contenedor.StartAsync();

        await using var contexto = CrearContexto();
        await contexto.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _contenedor.DisposeAsync().AsTask();

    /// <summary>
    /// Crea un contexto nuevo contra el contenedor. Cada prueba usa su propio contexto
    /// para que el seguimiento de cambios de una no contamine a las demás.
    /// </summary>
    /// <param name="reloj">Reloj a usar; por omisión, el de la fixture.</param>
    public LicitacionesDbContext CrearContexto(IClock? reloj = null)
    {
        var opciones = new DbContextOptionsBuilder<LicitacionesDbContext>()
            .UseNpgsql(CadenaConexion)
            .Options;

        return new LicitacionesDbContext(opciones, reloj ?? Reloj);
    }
}

/// <summary>Agrupa las pruebas que comparten el mismo contenedor de PostgreSQL.</summary>
[CollectionDefinition(Nombre)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Nombre = "PostgreSQL";
}
