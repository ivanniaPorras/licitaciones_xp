using Licitaciones.Domain.Dinero;
using Licitaciones.Domain.Licitaciones;
using Licitaciones.Domain.Ofertas;
using Licitaciones.Domain.Proveedores;
using Licitaciones.Infrastructure.Persistencia.Repositorios;
using Licitaciones.IntegrationTests.Apoyo;

namespace Licitaciones.IntegrationTests.Persistencia;

/// <summary>
/// Verifica los repositorios y la unidad de trabajo contra la base real, en particular
/// que el aprobador se resuelva consultando la tabla de niveles y que una transacción
/// fallida no deje cambios aplicados.
/// </summary>
[Collection(PostgresCollection.Nombre)]
public sealed class RepositoriosTests
{
    private readonly PostgresFixture _postgres;

    public RepositoriosTests(PostgresFixture postgres) => _postgres = postgres;

    [Theory]
    [InlineData(0.01, "Encargado de área")]
    [InlineData(500_000.00, "Encargado de área")]
    [InlineData(999_999.99, "Encargado de área")]
    [InlineData(1_000_000.00, "Gerencia")]
    [InlineData(9_999_999.99, "Gerencia")]
    [InlineData(10_000_000.00, "Junta Directiva")]
    [InlineData(50_000_000.00, "Junta Directiva")]
    public async Task ElAprobadorSeResuelveConsultandoLaTabla(decimal monto, string esperado)
    {
        await using var contexto = _postgres.CrearContexto();
        var repositorio = new NivelAprobacionRepository(contexto);

        var nivel = await repositorio.ObtenerAplicableAsync(MontoCRC.Crear(monto));

        Assert.NotNull(nivel);
        Assert.Equal(esperado, nivel.Aprobador);
    }

    [Fact]
    public async Task ExisteCodigo_ReconoceUnCodigoEscritoDeOtraForma()
    {
        var codigo = $"LIC-{Guid.NewGuid():N}"[..12];
        await using var contexto = _postgres.CrearContexto();
        var repositorio = new LicitacionRepository(contexto);
        var unidad = new UnitOfWork(contexto);

        repositorio.Agregar(Licitacion.Crear(
            codigo,
            "Licitación",
            1_000_000.00m,
            new DateTimeOffset(2026, 12, 31, 17, 0, 0, TimeSpan.Zero)));
        await unidad.GuardarCambiosAsync();

        Assert.True(await repositorio.ExisteCodigoAsync($"  {codigo.ToLowerInvariant()}  "));
        Assert.False(await repositorio.ExisteCodigoAsync($"LIC-{Guid.NewGuid():N}"[..12]));
    }

    [Fact]
    public async Task ExisteNombre_NoSeCompararConSigoMismoAlEditar()
    {
        var nombre = $"Proveedor {Guid.NewGuid():N}"[..25];
        await using var contexto = _postgres.CrearContexto();
        var repositorio = new ProveedorRepository(contexto);
        var unidad = new UnitOfWork(contexto);

        var proveedor = Proveedor.Crear(nombre);
        repositorio.Agregar(proveedor);
        await unidad.GuardarCambiosAsync();

        Assert.True(await repositorio.ExisteNombreAsync(nombre));
        Assert.False(await repositorio.ExisteNombreAsync(nombre, excluyendoId: proveedor.Id));
    }

    [Fact]
    public async Task ObtenerMontoMaximo_DevuelveLaOfertaMasAlta()
    {
        var (licitacion, proveedores) = await SembrarConOfertasAsync([700_000m, 1_200_000m, 950_000m]);

        await using var contexto = _postgres.CrearContexto();
        var repositorio = new OfertaRepository(contexto);

        var maximo = await repositorio.ObtenerMontoMaximoAsync(licitacion.Id);

        Assert.NotNull(maximo);
        Assert.Equal(1_200_000m, maximo.Value.Valor);
        Assert.Equal(3, proveedores.Count);
    }

    [Fact]
    public async Task ObtenerMontoMaximo_SinOfertas_DevuelveNulo()
    {
        var licitacion = NuevaLicitacion();
        await using var contexto = _postgres.CrearContexto();
        new LicitacionRepository(contexto).Agregar(licitacion);
        await new UnitOfWork(contexto).GuardarCambiosAsync();

        var repositorio = new OfertaRepository(contexto);

        Assert.Null(await repositorio.ObtenerMontoMaximoAsync(licitacion.Id));
    }

    [Fact]
    public async Task GuardarCambios_TraduceElDuplicadoAUnMensajeControlado()
    {
        var nombre = $"Duplicado {Guid.NewGuid():N}"[..25];

        await using (var contexto = _postgres.CrearContexto())
        {
            new ProveedorRepository(contexto).Agregar(Proveedor.Crear(nombre));
            await new UnitOfWork(contexto).GuardarCambiosAsync();
        }

        await using var segundo = _postgres.CrearContexto();
        new ProveedorRepository(segundo).Agregar(Proveedor.Crear(nombre.ToUpperInvariant()));

        var error = await Assert.ThrowsAsync<Domain.Excepciones.ReglaNegocioException>(
            () => new UnitOfWork(segundo).GuardarCambiosAsync());

        Assert.Equal("Ya existe un proveedor con ese nombre.", error.Message);
    }

    [Fact]
    public async Task SiLaOperacionFalla_LaTransaccionNoDejaCambiosAplicados()
    {
        var nombreExistente = $"Ocupado {Guid.NewGuid():N}"[..25];

        await using (var contexto = _postgres.CrearContexto())
        {
            new ProveedorRepository(contexto).Agregar(Proveedor.Crear(nombreExistente));
            await new UnitOfWork(contexto).GuardarCambiosAsync();
        }

        var nombreQueNoDebeQuedar = $"Revertido {Guid.NewGuid():N}"[..25];

        await using var contexto2 = _postgres.CrearContexto();
        var repositorio = new ProveedorRepository(contexto2);
        var unidad = new UnitOfWork(contexto2);

        await Assert.ThrowsAsync<Domain.Excepciones.ReglaNegocioException>(() =>
            unidad.EjecutarEnTransaccionAsync<bool>(_ =>
            {
                // El primero es válido; el segundo choca con el índice único y hace fallar
                // toda la operación.
                repositorio.Agregar(Proveedor.Crear(nombreQueNoDebeQuedar));
                repositorio.Agregar(Proveedor.Crear(nombreExistente));
                return Task.FromResult(true);
            }));

        await using var lectura = _postgres.CrearContexto();
        Assert.False(await new ProveedorRepository(lectura).ExisteNombreAsync(nombreQueNoDebeQuedar));
    }

    private static Licitacion NuevaLicitacion() =>
        Licitacion.Crear(
            $"LIC-{Guid.NewGuid():N}"[..12],
            "Licitación de prueba",
            5_000_000.00m,
            new DateTimeOffset(2026, 12, 31, 17, 0, 0, TimeSpan.Zero));

    private async Task<(Licitacion Licitacion, List<Proveedor> Proveedores)> SembrarConOfertasAsync(
        decimal[] montos)
    {
        var licitacion = NuevaLicitacion();
        var proveedores = new List<Proveedor>();

        await using var contexto = _postgres.CrearContexto();
        contexto.Licitaciones.Add(licitacion);

        var fecha = new DateTimeOffset(2026, 9, 1, 9, 0, 0, TimeSpan.Zero);
        foreach (var monto in montos)
        {
            var proveedor = Proveedor.Crear($"Oferente {Guid.NewGuid():N}"[..25]);
            proveedores.Add(proveedor);
            contexto.Proveedores.Add(proveedor);
            contexto.Ofertas.Add(Oferta.Crear(licitacion.Id, proveedor.Id, monto, fecha));
            fecha = fecha.AddMinutes(10);
        }

        await contexto.SaveChangesAsync();

        return (licitacion, proveedores);
    }
}
