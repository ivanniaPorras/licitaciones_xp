namespace Licitaciones.Application.Comun;

/// <summary>
/// Resultado de una operación que puede fallar por una regla de negocio.
/// </summary>
/// <remarks>
/// Se devuelve un resultado en lugar de lanzar una excepción porque el rechazo de una
/// regla es una salida esperada del caso de uso, no una situación excepcional. Las
/// excepciones quedan para los fallos que nadie previó.
/// </remarks>
public class Result
{
    /// <summary>Construye el resultado a partir de su error, o de la ausencia de él.</summary>
    /// <param name="error">Error que impidió completar la operación, o <c>null</c>.</param>
    protected Result(ErrorAplicacion? error) => Error = error;

    /// <summary>Indica si la operación se completó.</summary>
    public bool EsCorrecto => Error is null;

    /// <summary>Error que impidió completar la operación, o <c>null</c> si salió bien.</summary>
    public ErrorAplicacion? Error { get; }

    /// <summary>Resultado correcto sin valor asociado.</summary>
    public static Result Correcto() => new(error: null);

    /// <summary>Resultado fallido.</summary>
    /// <param name="error">Error que impidió completar la operación.</param>
    public static Result Fallo(ErrorAplicacion error) => new(error);
}

/// <summary>Resultado de una operación que devuelve un valor.</summary>
/// <typeparam name="T">Tipo del valor devuelto.</typeparam>
public sealed class Result<T> : Result
{
    private Result(T? valor, ErrorAplicacion? error) : base(error) => Valor = valor;

    /// <summary>Valor producido por la operación, o <c>null</c> si falló.</summary>
    public T? Valor { get; }

    /// <summary>Resultado correcto con su valor.</summary>
    /// <param name="valor">Valor producido.</param>
    public static Result<T> Correcto(T valor) => new(valor, error: null);

    /// <summary>Resultado fallido.</summary>
    /// <param name="error">Error que impidió completar la operación.</param>
    public static new Result<T> Fallo(ErrorAplicacion error) => new(default, error);
}
