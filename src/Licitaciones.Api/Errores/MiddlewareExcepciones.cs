using Licitaciones.Application.Comun;
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

            // Una ruta que no existe o un verbo no admitido salen del enrutador con el
            // código puesto pero sin cuerpo. Se completan aquí para que ninguna respuesta
            // de error de la API llegue sin código propio ni identificador de correlación.
            await CompletarRespuestaSinCuerpoAsync(contexto);
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
        catch (OperationCanceledException) when (contexto.RequestAborted.IsCancellationRequested)
        {
            // La persona usuaria cerró la pestaña. No es un fallo del servidor y no debe
            // registrarse como tal ni intentar escribir en una respuesta ya abandonada.
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

    private static Task CompletarRespuestaSinCuerpoAsync(HttpContext contexto)
    {
        if (contexto.Response.HasStarted || contexto.Response.ContentLength > 0)
        {
            return Task.CompletedTask;
        }

        return contexto.Response.StatusCode switch
        {
            StatusCodes.Status404NotFound => EscribirAsync(
                contexto,
                StatusCodes.Status404NotFound,
                "Recurso no encontrado",
                "La ruta solicitada no existe en esta API.",
                CodigosError.RutaNoEncontrada),
            StatusCodes.Status405MethodNotAllowed => EscribirAsync(
                contexto,
                StatusCodes.Status405MethodNotAllowed,
                "Método no permitido",
                "Esa ruta no admite el verbo empleado.",
                CodigosError.MetodoNoPermitido),
            _ => Task.CompletedTask
        };
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
