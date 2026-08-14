using System.Reflection;
using Licitaciones.Application;
using Licitaciones.Domain;

namespace Licitaciones.UnitTests.Arquitectura;

/// <summary>
/// Verifica la dirección de dependencias declarada en la arquitectura: el dominio no
/// conoce a nadie y la capa de aplicación no conoce la infraestructura de persistencia.
/// </summary>
public sealed class DireccionDependenciasTests
{
    private static readonly string[] EnsambladosDeInfraestructura =
    [
        "Microsoft.EntityFrameworkCore",
        "Npgsql",
        "Microsoft.AspNetCore"
    ];

    [Fact]
    public void Dominio_NoReferenciaInfraestructura()
    {
        var referencias = NombresDeReferencias(DominioAssemblyReference.Assembly);

        Assert.DoesNotContain(referencias, EsInfraestructura);
    }

    [Fact]
    public void Aplicacion_NoReferenciaInfraestructura()
    {
        var referencias = NombresDeReferencias(AplicacionAssemblyReference.Assembly);

        Assert.DoesNotContain(referencias, EsInfraestructura);
    }

    [Fact]
    public void Dominio_NoReferenciaLaCapaDeAplicacion()
    {
        var referencias = NombresDeReferencias(DominioAssemblyReference.Assembly);

        Assert.DoesNotContain("Licitaciones.Application", referencias);
    }

    private static IReadOnlyList<string> NombresDeReferencias(Assembly ensamblado) =>
        [.. ensamblado.GetReferencedAssemblies().Select(r => r.Name ?? string.Empty)];

    private static bool EsInfraestructura(string nombreDeEnsamblado) =>
        EnsambladosDeInfraestructura.Any(prefijo =>
            nombreDeEnsamblado.StartsWith(prefijo, StringComparison.Ordinal));
}
