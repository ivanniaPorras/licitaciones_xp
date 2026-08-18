namespace Licitaciones.Application.Comun;

/// <summary>
/// Parámetros comunes a todos los listados: página, tamaño, orden y búsqueda.
/// </summary>
public abstract record ConsultaPaginada
{
    /// <summary>Cantidad máxima de elementos que se admite pedir de una vez.</summary>
    public const int TamanoMaximo = 100;

    /// <summary>Cantidad de elementos por página cuando no se indica otra.</summary>
    public const int TamanoPorDefecto = 20;

    private readonly int _pagina = 1;
    private readonly int _tamano = TamanoPorDefecto;

    /// <summary>Número de página, empezando en 1. Los valores menores se corrigen a 1.</summary>
    public int Pagina
    {
        get => _pagina;
        init => _pagina = value < 1 ? 1 : value;
    }

    /// <summary>
    /// Elementos por página. Se acota entre 1 y <see cref="TamanoMaximo"/> para que una
    /// petición no pueda pedir la tabla entera.
    /// </summary>
    public int Tamano
    {
        get => _tamano;
        init => _tamano = value switch
        {
            < 1 => TamanoPorDefecto,
            > TamanoMaximo => TamanoMaximo,
            _ => value
        };
    }

    /// <summary>Campo y dirección de ordenamiento, por ejemplo <c>nombre:desc</c>.</summary>
    public string? Orden { get; init; }

    /// <summary>Término de búsqueda textual.</summary>
    public string? Busqueda { get; init; }
}
