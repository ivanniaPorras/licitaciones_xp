using Licitaciones.Application.Comun;
using Licitaciones.Application.Proveedores;
using Licitaciones.Domain.Proveedores;
using Licitaciones.UnitTests.Apoyo.Dobles;

namespace Licitaciones.UnitTests.Aplicacion;

/// <summary>
/// Verifica las reglas del servicio de proveedores: unicidad del nombre normalizado,
/// caracteres admitidos y baja lógica cuando existen ofertas relacionadas
/// (HU-013, HU-014).
/// </summary>
public sealed class ProveedorServiceTests
{
    private readonly RepositorioProveedoresEnMemoria _proveedores = new();
    private readonly RepositorioOfertasEnMemoria _ofertas = new();
    private readonly UnidadDeTrabajoFalsa _unidad = new();

    private ProveedorService CrearServicio() => new(_proveedores, _ofertas, _unidad);

    [Fact]
    public async Task Crear_ConNombreValido_DevuelveElProveedorCreado()
    {
        var resultado = await CrearServicio().CrearAsync(new CrearProveedorRequest("Empresa Central S.A."));

        Assert.True(resultado.EsCorrecto);
        Assert.Equal("Empresa Central S.A.", resultado.Valor!.Nombre);
        Assert.NotEqual(Guid.Empty, resultado.Valor.Id);
        Assert.Equal(1, _unidad.Confirmaciones);
    }

    [Fact]
    public async Task Crear_ConNombreEquivalenteAUnoExistente_EsRechazado()
    {
        _proveedores.Sembrar(Proveedor.Crear("Empresa Central"));

        var resultado = await CrearServicio().CrearAsync(new CrearProveedorRequest("  EMPRESA   central  "));

        Assert.False(resultado.EsCorrecto);
        Assert.Equal(CodigosError.ProveedorDuplicado, resultado.Error!.Codigo);
        Assert.Equal(TipoError.Conflicto, resultado.Error.Tipo);
        Assert.Equal("Ya existe un proveedor con ese nombre.", resultado.Error.Mensaje);
    }

    [Theory]
    [InlineData("Empresa@Central")]
    [InlineData("Empresa & Cía")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Crear_ConNombreInvalido_EsRechazado(string nombre)
    {
        var resultado = await CrearServicio().CrearAsync(new CrearProveedorRequest(nombre));

        Assert.False(resultado.EsCorrecto);
        Assert.Equal(CodigosError.NombreProveedorInvalido, resultado.Error!.Codigo);
        Assert.Equal(TipoError.Validacion, resultado.Error.Tipo);
    }

    [Fact]
    public async Task Crear_NoGuardaCuandoElNombreEsInvalido()
    {
        await CrearServicio().CrearAsync(new CrearProveedorRequest("Empresa@Central"));

        Assert.Empty(_proveedores.Contenido);
        Assert.Equal(0, _unidad.Confirmaciones);
    }

    [Fact]
    public async Task Actualizar_ConservandoSuPropioNombre_EsAceptado()
    {
        var proveedor = Proveedor.Crear("Constructora del Valle");
        _proveedores.Sembrar(proveedor);

        var resultado = await CrearServicio().ActualizarAsync(
            proveedor.Id,
            new ActualizarProveedorRequest("Constructora del Valle"));

        Assert.True(resultado.EsCorrecto);
    }

    [Fact]
    public async Task Actualizar_ConElNombreDeOtroProveedor_EsRechazado()
    {
        var primero = Proveedor.Crear("Empresa Central");
        var segundo = Proveedor.Crear("Constructora del Valle");
        _proveedores.Sembrar(primero, segundo);

        var resultado = await CrearServicio().ActualizarAsync(
            segundo.Id,
            new ActualizarProveedorRequest("empresa central"));

        Assert.False(resultado.EsCorrecto);
        Assert.Equal(CodigosError.ProveedorDuplicado, resultado.Error!.Codigo);
    }

    [Fact]
    public async Task Actualizar_UnProveedorInexistente_DevuelveNoEncontrado()
    {
        var resultado = await CrearServicio().ActualizarAsync(
            Guid.NewGuid(),
            new ActualizarProveedorRequest("Cualquiera"));

        Assert.False(resultado.EsCorrecto);
        Assert.Equal(TipoError.NoEncontrado, resultado.Error!.Tipo);
    }

    [Fact]
    public async Task Eliminar_UnProveedorSinOfertas_LoDaDeBaja()
    {
        var proveedor = Proveedor.Crear("Sin ofertas");
        _proveedores.Sembrar(proveedor);

        var resultado = await CrearServicio().EliminarAsync(proveedor.Id);

        Assert.True(resultado.EsCorrecto);
        Assert.Empty(_proveedores.Contenido);
    }

    [Fact]
    public async Task Eliminar_UnProveedorConOfertas_TambienLoDaDeBajaYConservaLasOfertas()
    {
        var proveedor = Proveedor.Crear("Con ofertas");
        _proveedores.Sembrar(proveedor);
        _ofertas.Sembrar(Domain.Ofertas.Oferta.Crear(
            Guid.NewGuid(),
            proveedor.Id,
            500_000m,
            new DateTimeOffset(2026, 9, 1, 9, 0, 0, TimeSpan.Zero)));

        var resultado = await CrearServicio().EliminarAsync(proveedor.Id);

        // El borrado es lógico, de modo que la oferta nunca queda huérfana.
        Assert.True(resultado.EsCorrecto);
        Assert.Single(_ofertas.Contenido);
    }

    [Fact]
    public async Task Obtener_UnProveedorInexistente_DevuelveNoEncontrado()
    {
        var resultado = await CrearServicio().ObtenerAsync(Guid.NewGuid());

        Assert.False(resultado.EsCorrecto);
        Assert.Equal(TipoError.NoEncontrado, resultado.Error!.Tipo);
    }

    [Fact]
    public async Task Listar_DevuelveLaPaginaSolicitadaConSuTotal()
    {
        _proveedores.Sembrar(
            Proveedor.Crear("Alfa"),
            Proveedor.Crear("Beta"),
            Proveedor.Crear("Gamma"),
            Proveedor.Crear("Delta"));

        var resultado = await CrearServicio().ListarAsync(new ConsultaProveedores(Pagina: 2, Tamano: 2));

        Assert.True(resultado.EsCorrecto);
        Assert.Equal(4, resultado.Valor!.Total);
        Assert.Equal(2, resultado.Valor.TotalPaginas);
        Assert.Equal(2, resultado.Valor.Elementos.Count);
    }

    [Fact]
    public async Task Listar_FiltraPorElTerminoDeBusquedaIgnorandoMayusculas()
    {
        _proveedores.Sembrar(Proveedor.Crear("Constructora del Valle"), Proveedor.Crear("Empresa Central"));

        var resultado = await CrearServicio().ListarAsync(new ConsultaProveedores(Busqueda: "CENTRAL"));

        Assert.Single(resultado.Valor!.Elementos);
        Assert.Equal("Empresa Central", resultado.Valor.Elementos[0].Nombre);
    }

    [Fact]
    public async Task ObtenerOfertas_DevuelveLasDelProveedor()
    {
        var proveedor = Proveedor.Crear("Oferente");
        _proveedores.Sembrar(proveedor);
        _ofertas.Sembrar(
            Domain.Ofertas.Oferta.Crear(Guid.NewGuid(), proveedor.Id, 100_000m,
                new DateTimeOffset(2026, 9, 1, 9, 0, 0, TimeSpan.Zero)),
            Domain.Ofertas.Oferta.Crear(Guid.NewGuid(), Guid.NewGuid(), 200_000m,
                new DateTimeOffset(2026, 9, 2, 9, 0, 0, TimeSpan.Zero)));

        var resultado = await CrearServicio().ObtenerOfertasAsync(proveedor.Id);

        Assert.True(resultado.EsCorrecto);
        Assert.Single(resultado.Valor!);
    }
}
