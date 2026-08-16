using Licitaciones.Domain.Dinero;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Licitaciones.Infrastructure.Persistencia.Configuraciones;

/// <summary>
/// Conversiones entre los objetos de valor monetarios del dominio y las columnas
/// numéricas de PostgreSQL.
/// </summary>
internal static class ConversoresDinero
{
    /// <summary>Convierte un <see cref="MontoCRC"/> a <c>numeric</c> y de vuelta.</summary>
    public static readonly ValueConverter<MontoCRC, decimal> Monto =
        new(monto => monto.Valor, valor => MontoCRC.Crear(valor));

    /// <summary>Convierte un <see cref="MontoCRC"/> opcional, usado en el rango abierto.</summary>
    public static readonly ValueConverter<MontoCRC?, decimal?> MontoOpcional =
        new(monto => monto == null ? null : monto.Value.Valor,
            valor => valor == null ? null : MontoCRC.Crear(valor.Value));
}
