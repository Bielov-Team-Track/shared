using Microsoft.AspNetCore.Http;

namespace Shared.Services;

internal static class GuardianRequestClassification
{
    /// <summary>
    /// A removal notice blocks writes and lets reads through. Shared by the authorizer and by
    /// the middleware's compatibility branch so the two can never disagree about which verbs
    /// count as a write.
    /// </summary>
    internal static bool IsWriteAction(HttpRequest request) =>
        request.Method is "POST" or "PUT" or "PATCH" or "DELETE";
}
