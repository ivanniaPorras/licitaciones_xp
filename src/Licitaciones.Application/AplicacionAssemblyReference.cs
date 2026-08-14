namespace Licitaciones.Application;

/// <summary>
/// Punto de anclaje del ensamblado de aplicación. Permite localizarlo por reflexión desde
/// las pruebas de arquitectura sin acoplarse a ningún caso de uso concreto.
/// </summary>
public static class AplicacionAssemblyReference
{
    /// <summary>Ensamblado que contiene la capa de aplicación.</summary>
    public static readonly System.Reflection.Assembly Assembly =
        typeof(AplicacionAssemblyReference).Assembly;
}
