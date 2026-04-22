namespace Frontend.Services;

public static class PageErrorHelper
{
    public static string Describe(Exception ex)
    {
        var msg = $"{ex.GetType().Name}: {ex.Message}";
        var mixed =
            " If you use an https:// Blazor URL, the browser may block calls to an http:// gateway (mixed content). Prefer the http launch profile (e.g. http://localhost:5066) or use an https gateway.";
        if (ex.Message.Contains("Load failed", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("Failed to fetch", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("NetworkError", StringComparison.OrdinalIgnoreCase))
            return $"Could not reach the API gateway (check wwwroot/appsettings.json Api:BaseUrl). {msg}.{mixed}";
        return $"Request failed. {msg}";
    }
}
