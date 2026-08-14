namespace Licitaciones.Domain;

/// <summary>
/// Punto de anclaje del ensamblado de dominio. Permite localizarlo por reflexión desde
/// las pruebas de arquitectura sin acoplarse a ningún tipo de negocio concreto.
/// </summary>
public static class DominioAssemblyReference
{
    /// <summary>Ensamblado que contiene la capa de dominio.</summary>
    public static readonly System.Reflection.Assembly Assembly =
        typeof(DominioAssemblyReference).Assembly;
}
