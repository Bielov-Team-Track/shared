namespace Shared.Services.FileStorage;

public static class ContentDispositionValue
{
    /// <summary>
    /// Builds an RFC 6266 "inline" Content-Disposition value so browsers render the
    /// file in-tab and still get a sensible name on save. Emits an ASCII-safe
    /// filename plus an RFC 5987 UTF-8 filename* variant when the name needs one.
    /// </summary>
    public static string Inline(string? fileName)
    {
        var sanitized = fileName == null
            ? string.Empty
            : new string(fileName.Where(c => !char.IsControl(c)).ToArray()).Trim();
        if (sanitized.Length == 0)
            return "inline";

        var fallback = new string(sanitized.Select(c => c is '"' or '\\' || c > '\x7e' ? '_' : c).ToArray());
        var value = $"inline; filename=\"{fallback}\"";
        return fallback == sanitized
            ? value
            : $"{value}; filename*=UTF-8''{Uri.EscapeDataString(sanitized)}";
    }
}
