namespace EnfoqueAutomotriz.UI.Helpers;

/// <summary>
/// Normaliza URLs de fotos/archivos almacenadas en BD para usarlas con el proxy /api-uploads/.
/// Las URLs externas absolutas (http/https, ej. un logo enlazado desde otro sitio) se devuelven
/// tal cual, sin pasar por el proxy — solo las rutas locales relativas ("uploads/...") se reescriben.
/// </summary>
public static class FotoUrlHelper
{
    public static string? ToProxy(string? storedUrl)
    {
        if (string.IsNullOrEmpty(storedUrl))
            return null;

        if (storedUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            storedUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            storedUrl.StartsWith("//", StringComparison.Ordinal))
            return storedUrl;

        var path = storedUrl.TrimStart('/');

        if (path.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
            path = path["uploads/".Length..];

        return $"/api-uploads/{path}";
    }
}
