using System;
using System.Collections.Generic;
using System.Text;

namespace Facturama.Sdk.Internal;

/// <summary>
/// Validaciones de argumentos portables entre targets.
/// <c>ArgumentNullException.ThrowIfNull</c> es net6+ y no existe en netstandard2.0.
/// </summary>
internal static class Guard
{
    private const string BlankMessage = "El valor no puede estar vacío.";
    private const string NavigationSegmentMessage = "El valor no puede ser '.' ni '..'.";

    /// <summary>
    /// Lanza <see cref="ArgumentNullException"/> cuando <paramref name="value"/> es nulo.
    /// </summary>
    /// 
    internal static void NotNull<T>(T? value, string paramName)
        where T : class
    {
        if (value is null)
        {
            throw new ArgumentNullException(paramName);
        }
    }

    /// <summary>
    /// Lanza <see cref="ArgumentNullException"/> si <paramref name="value"/> es nulo y
    /// <see cref="ArgumentException"/> si está vacío o solo contiene espacios.
    /// </summary>
    /// 
    internal static void NotNullOrWhiteSpace(string? value, string paramName)
    {
        if(value is null)
        {
            throw new ArgumentNullException(paramName);
        }
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(BlankMessage, paramName);
        }
    }

    /// <summary>
    /// Valida <paramref name="value"/> y lo devuelve codificado para usarse como un
    /// <b>único segmento</b> de ruta: <c>/</c>, <c>?</c>, <c>#</c> y espacios no pueden
    /// añadir segmentos, inyectar una query ni truncar la URL. Rechaza <c>.</c> y <c>..</c>
    /// porque no se codifican y el cliente HTTP los colapsaría como navegación de rutas.
    /// </summary>
    /// 
    internal static string PathSegment(string? value, string paramName)
    {
        NotNullOrWhiteSpace(value, paramName);

        if (value is "." or "..")
            throw new ArgumentException(NavigationSegmentMessage, paramName);

        return Uri.EscapeDataString(value!);
    }
}
