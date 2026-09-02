namespace Shared.Services.Analytics;

/// <summary>
/// Which client made the request, as the client declares it in <see cref="HeaderName"/>.
/// An absent or unrecognised header is <see cref="Unknown"/> and never a guess: the whole
/// value of the property is answering "did this club come from the browser or the app".
/// </summary>
public static class ClientSource
{
    public const string HeaderName = "X-Client-Source";

    public const string Web = "web";
    public const string Ios = "ios";
    public const string Android = "android";
    public const string Unknown = "unknown";

    public static string Normalize(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        Web => Web,
        Ios => Ios,
        Android => Android,
        _ => Unknown
    };
}
