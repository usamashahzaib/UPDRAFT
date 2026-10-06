namespace HotdropBackend.Services;

/// <summary>Release links returned when the client protocol is below the server minimum.</summary>
public sealed class CompatibilityOptions
{
    public string AndroidStoreUrl { get; set; } = string.Empty;
    public string IosStoreUrl { get; set; } = string.Empty;

    public string StoreUrl(string platform)
    {
        var configured = platform.Trim().ToLowerInvariant() switch
        {
            "android" => AndroidStoreUrl,
            "ios" => IosStoreUrl,
            _ => string.Empty,
        };
        return Uri.TryCreate(configured, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps
            ? uri.AbsoluteUri
            : string.Empty;
    }
}
