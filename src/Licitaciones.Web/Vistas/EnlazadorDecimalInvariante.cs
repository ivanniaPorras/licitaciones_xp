using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Licitaciones.Web.Vistas;

/// <summary>
/// Interpreta los decimales que llegan de un formulario con el punto como separador
/// decimal, con independencia de la cultura con la que se muestran.
/// </summary>
/// <remarks>
/// La aplicación presenta los montos con la cultura de Costa Rica, donde el punto separa
/// los miles y la coma los decimales. Pero un campo <c>input type="number"</c> **siempre**
/// envía el valor en formato invariante, con punto decimal y sin separador de miles. Al
/// interpretarlo con es-CR, un presupuesto de 10 000 000,00 llega como
/// <c>"10000000.00"</c>, cuyo último grupo tiene dos dígitos en lugar de tres, y la
/// conversión falla: el monto se enlaza como cero y el formulario rechaza el dato que la
/// persona escribió bien.
///
/// Por eso se prueba primero la cultura invariante y solo después la del sitio, que sigue
/// admitiendo un valor escrito a mano con coma decimal.
/// </remarks>
public sealed class EnlazadorDecimalInvariante : IModelBinder
{
    /// <inheritdoc />
    public Task BindModelAsync(ModelBindingContext contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);

        var valor = contexto.ValueProvider.GetValue(contexto.ModelName);
        if (valor == ValueProviderResult.None)
        {
            return Task.CompletedTask;
        }

        contexto.ModelState.SetModelValue(contexto.ModelName, valor);

        var texto = valor.FirstValue;
        if (string.IsNullOrWhiteSpace(texto))
        {
            // Un campo opcional que llega vacío no es un error: se queda sin valor.
            if (Nullable.GetUnderlyingType(contexto.ModelType) is not null)
            {
                contexto.Result = ModelBindingResult.Success(null);
            }

            return Task.CompletedTask;
        }

        const NumberStyles Estilos = NumberStyles.Float | NumberStyles.AllowThousands;

        if (decimal.TryParse(texto, Estilos, CultureInfo.InvariantCulture, out var numero)
            || decimal.TryParse(texto, Estilos, CultureInfo.CurrentCulture, out numero))
        {
            contexto.Result = ModelBindingResult.Success(numero);
            return Task.CompletedTask;
        }

        contexto.ModelState.TryAddModelError(contexto.ModelName, "El valor no es un número válido.");

        return Task.CompletedTask;
    }
}

/// <summary>Aplica <see cref="EnlazadorDecimalInvariante"/> a todo decimal.</summary>
public sealed class ProveedorEnlazadorDecimalInvariante : IModelBinderProvider
{
    /// <inheritdoc />
    public IModelBinder? GetBinder(ModelBinderProviderContext contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);

        var tipo = contexto.Metadata.ModelType;

        return tipo == typeof(decimal) || tipo == typeof(decimal?)
            ? new EnlazadorDecimalInvariante()
            : null;
    }
}
