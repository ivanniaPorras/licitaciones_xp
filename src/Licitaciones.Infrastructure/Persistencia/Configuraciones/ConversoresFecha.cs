using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Licitaciones.Infrastructure.Persistencia.Configuraciones;

/// <summary>
/// Lleva toda fecha a tiempo universal antes de guardarla en una columna
/// <c>timestamp with time zone</c>.
/// </summary>
/// <remarks>
/// PostgreSQL almacena el instante, no el huso horario con que se escribió, y el
/// controlador rechaza cualquier desplazamiento distinto de cero. Normalizar aquí, en el
/// límite de la persistencia, hace que la regla se cumpla sola: quien registra una oferta
/// a las 08:30 en Costa Rica no tiene que acordarse de convertirla, y todas las
/// comparaciones posteriores operan sobre la misma referencia.
/// </remarks>
internal sealed class ConversorFechaAUtc : ValueConverter<DateTimeOffset, DateTimeOffset>
{
    public ConversorFechaAUtc()
        : base(fecha => fecha.ToUniversalTime(), fecha => fecha)
    {
    }
}

/// <summary>
/// Variante de <see cref="ConversorFechaAUtc"/> para las fechas opcionales, como la del
/// borrado lógico.
/// </summary>
internal sealed class ConversorFechaOpcionalAUtc : ValueConverter<DateTimeOffset?, DateTimeOffset?>
{
    public ConversorFechaOpcionalAUtc()
        : base(fecha => fecha == null ? null : fecha.Value.ToUniversalTime(), fecha => fecha)
    {
    }
}
