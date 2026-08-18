using Licitaciones.Application.Comun;
using Microsoft.AspNetCore.Mvc;

namespace Licitaciones.Api.Comun;

/// <summary>
/// Base de los controladores de la API. Traduce el resultado de un caso de uso al código
/// HTTP que le corresponde, de modo que ningún controlador tenga que decidirlo por su
/// cuenta.
/// </summary>
[ApiController]
public abstract class ControladorApiBase : ControllerBase
{
    /// <summary>Convierte un error de aplicación en la respuesta HTTP correspondiente.</summary>
    /// <param name="error">Error devuelto por el caso de uso.</param>
    protected ActionResult AProblema(ErrorAplicacion error)
    {
        ArgumentNullException.ThrowIfNull(error);

        // 404 recurso inexistente, 409 el estado actual impide la operación,
        // 422 los datos son interpretables pero violan una regla.
        var estado = error.Tipo switch
        {
            TipoError.NoEncontrado => StatusCodes.Status404NotFound,
            TipoError.Conflicto => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status422UnprocessableEntity
        };

        var problema = new ProblemDetails
        {
            Title = TituloDe(error.Tipo),
            Status = estado,
            Detail = error.Mensaje,
            Instance = HttpContext.Request.Path
        };

        problema.Extensions["code"] = error.Codigo;
        problema.Extensions["correlationId"] = HttpContext.TraceIdentifier;

        return StatusCode(estado, problema);
    }

    private static string TituloDe(TipoError tipo) => tipo switch
    {
        TipoError.NoEncontrado => "Recurso no encontrado",
        TipoError.Conflicto => "Conflicto con el estado actual",
        _ => "Regla de negocio incumplida"
    };
}
