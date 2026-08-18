namespace Licitaciones.Application.Comun;

/// <summary>
/// Error de negocio listo para mostrarse: un código estable que los clientes pueden
/// interpretar y un mensaje redactado para la persona usuaria.
/// </summary>
/// <param name="Codigo">Código estable del dominio, por ejemplo <c>OFERTA_DUPLICADA</c>.</param>
/// <param name="Mensaje">Texto seguro que se muestra sin revelar detalles internos.</param>
/// <param name="Tipo">Naturaleza del error, que decide el código HTTP.</param>
public sealed record ErrorAplicacion(string Codigo, string Mensaje, TipoError Tipo)
{
    /// <summary>Crea un error de validación de regla de negocio.</summary>
    public static ErrorAplicacion Validacion(string codigo, string mensaje) =>
        new(codigo, mensaje, TipoError.Validacion);

    /// <summary>Crea un error de conflicto con el estado actual del sistema.</summary>
    public static ErrorAplicacion Conflicto(string codigo, string mensaje) =>
        new(codigo, mensaje, TipoError.Conflicto);

    /// <summary>Crea un error de recurso inexistente.</summary>
    public static ErrorAplicacion NoEncontrado(string codigo, string mensaje) =>
        new(codigo, mensaje, TipoError.NoEncontrado);
}
