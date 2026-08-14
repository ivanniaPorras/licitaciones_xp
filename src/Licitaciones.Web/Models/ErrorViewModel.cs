namespace Licitaciones.Web.Models;

/// <summary>
/// Datos mínimos que se muestran al usuario cuando ocurre un error no controlado.
/// </summary>
public sealed class ErrorViewModel
{
    /// <summary>Identificador de correlación de la solicitud que falló.</summary>
    public string? RequestId { get; init; }

    /// <summary>Indica si hay un identificador de correlación que mostrar.</summary>
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
