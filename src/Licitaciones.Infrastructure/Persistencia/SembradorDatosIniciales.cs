using Licitaciones.Domain.Aprobacion;
using Licitaciones.Domain.Dinero;
using Microsoft.EntityFrameworkCore;

namespace Licitaciones.Infrastructure.Persistencia;

/// <summary>
/// Datos con los que el sistema queda operativo desde la primera ejecución, sin que nadie
/// tenga que cargarlos a mano y sin acceso a Internet.
/// </summary>
internal static class SembradorDatosIniciales
{
    // Los identificadores y las fechas son constantes literales, no valores calculados:
    // las migraciones deben producir siempre el mismo script, y un Guid.NewGuid() o un
    // DateTimeOffset.UtcNow generarían una migración distinta en cada ejecución.
    private static readonly DateTimeOffset Origen = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    /// <summary>Declara los datos iniciales en el modelo.</summary>
    /// <param name="modelo">Constructor del modelo de Entity Framework Core.</param>
    public static void Sembrar(ModelBuilder modelo)
    {
        modelo.Entity<NivelAprobacion>().HasData(
            Nivel("6f2b1c4a-0000-4000-8000-000000000001", 0.01m, 999_999.99m, "Encargado de área"),
            Nivel("6f2b1c4a-0000-4000-8000-000000000002", 1_000_000.00m, 9_999_999.99m, "Gerencia"),
            Nivel("6f2b1c4a-0000-4000-8000-000000000003", 10_000_000.00m, null, "Junta Directiva"));

        modelo.Entity<TipoCambio>().HasData(new
        {
            Id = Guid.Parse("7a3c2d5b-0000-4000-8000-000000000001"),
            CRCporUSD = 512.0000m,
            FechaVigencia = Origen,
            Activo = true,
            CreatedAt = Origen,
            UpdatedAt = Origen
        });
    }

    // Los valores se declaran con el tipo del dominio y no con el de la columna: la
    // semilla se define sobre el modelo, y es el conversor quien la lleva a numeric.
    private static object Nivel(string id, decimal minimo, decimal? maximo, string aprobador) => new
    {
        Id = Guid.Parse(id),
        MontoMinimo = MontoCRC.Crear(minimo),
        MontoMaximo = maximo is null ? null : (MontoCRC?)MontoCRC.Crear(maximo.Value),
        Aprobador = aprobador,
        CreatedAt = Origen,
        UpdatedAt = Origen
    };
}
