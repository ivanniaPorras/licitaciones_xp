using Licitaciones.Domain.Excepciones;
using Microsoft.AspNetCore.Mvc;

namespace Licitaciones.Api.Errores;

/// <summary>
/// Convierte cualquier fallo no controlado en una respuesta <c>ProblemDetails</c>
/// sanitizada.
/// </summary>
/// <remarks>
/// El detalle completo queda en el registro del servidor junto al identificador de
/// correlación; al cliente solo se le devuelve un mensaje seguro. Nunca salen trazas de
/// pila, rutas internas, consultas ni credenciales.
/// </remarks>
public sealed class MiddlewareExcepciones
{
    private readonly RequestDelegate _siguiente;
    private readonly ILogger<MiddlewareExcepciones> _registro;

    /// <summary>Crea el middleware.</summary>
    /// <param name="siguiente">Siguiente paso de la canalización.</param>
    /// <param name="registro">Registro de sucesos del servidor.</param>
    public MiddlewareExcepciones(RequestDelegate siguiente, ILogger<MiddlewareExcepciones> registro)
    {
        _siguiente = siguiente;
        _registro = registro;
    }

    /// <summary>Ejecuta el middleware.</summary>
    /// <param name="contexto">Contexto de la petición.</param>
    public async Task InvokeAsync(HttpContext contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);

        try
        {
            await _siguiente(contexto);
        }
        catch (ReglaNegocioException error)
        {
            // Una regla que se escapó de la capa de aplicación: su mensaje ya es seguro.
            _registro.LogWarning(
                error,
                "Regla de negocio incumplida. Correlación {CorrelationId}",
                contexto.TraceIdentifier);

            await EscribirAsync(contexto, StatusCodes.Status409Conflict, "Conflicto con el estado actual",
                error.Message, "REGLA_NEGOCIO");
        }
        catch (Exception error)
        {
            _registro.LogError(
                error,
                "Error no controlado. Correlación {CorrelationId}",
                contexto.TraceIdentifier);

            await EscribirAsync(contexto, StatusCodes.Status500InternalServerError, "Error interno",
                "Ocurrió un error inesperado. Intente de nuevo o contacte a soporte.", "ERROR_INTERNO");
        }
    }

    private static async Task EscribirAsync(
        HttpContext contexto,
        int estado,
        string titulo,
        string detalle,
        string codigo)
    {
        if (contexto.Response.HasStarted)
        {
            return;
        }

        var problema = new ProblemDetails
        {
            Title = titulo,
            Status = estado,
            Detail = detalle,
            Instance = contexto.Request.Path
        };

        problema.Extensions["code"] = codigo;
        problema.Extensions["correlationId"] = contexto.TraceIdentifier;

        contexto.Response.Clear();
        contexto.Response.StatusCode = estado;
        contexto.Response.ContentType = "application/problem+json";

        await contexto.Response.WriteAsJsonAsync(problema);
    }
}
