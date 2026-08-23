using Licitaciones.Application.Comun;
using Microsoft.AspNetCore.Mvc;

namespace Licitaciones.Api.Errores;

/// <summary>
/// Construye la respuesta 400 de una solicitud que ni siquiera se pudo interpretar.
/// </summary>
/// <remarks>
/// La respuesta que ASP.NET Core produce por omisión no lleva el código propio del dominio
/// ni el identificador de correlación, y arrastra los mensajes del enlazador de modelos,
/// que están en inglés y describen tipos internos. Aquí se sustituye por un cuerpo con la
/// misma forma que el resto de los errores del sistema, nombrando solo los campos que
/// fallaron.
/// </remarks>
public static class RespuestaSolicitudInvalida
{
    /// <summary>Convierte el estado del modelo en un <c>ProblemDetails</c> seguro.</summary>
    /// <param name="contexto">Contexto de la acción con el estado del modelo.</param>
    public static IActionResult Crear(ActionContext contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);

        var campos = contexto.ModelState
            .Where(entrada => entrada.Value?.Errors.Count > 0)
            .Select(entrada => string.IsNullOrEmpty(entrada.Key) ? "cuerpo de la solicitud" : entrada.Key)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var problema = new ProblemDetails
        {
            Title = "Solicitud mal formada",
            Status = StatusCodes.Status400BadRequest,
            Detail = campos.Count == 0
                ? "La solicitud no se pudo interpretar."
                : $"La solicitud no se pudo interpretar. Revise: {string.Join(", ", campos)}.",
            Instance = contexto.HttpContext.Request.Path
        };

        problema.Extensions["code"] = CodigosError.SolicitudInvalida;
        problema.Extensions["correlationId"] = contexto.HttpContext.TraceIdentifier;

        return new ObjectResult(problema)
        {
            StatusCode = StatusCodes.Status400BadRequest,
            ContentTypes = { "application/problem+json" }
        };
    }
}
