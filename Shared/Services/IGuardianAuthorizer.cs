using Microsoft.AspNetCore.Http;
using Shared.Enums;

namespace Shared.Services;

public interface IGuardianAuthorizer
{
    /// <summary>
    /// Throws ForbiddenException unless <paramref name="actorUserId"/> may act for
    /// <paramref name="subjectUserId"/> on this request. Never returns false — the failure IS
    /// the exception, so a caller cannot forget to check a bool.
    /// </summary>
    Task<GuardianAuthorization> AuthorizeAsync(
        Guid actorUserId,
        Guid subjectUserId,
        GuardianPermission requiredPermission,
        ConsentType? requiredConsent,
        HttpRequest request,
        CancellationToken ct = default);
}

public sealed record GuardianAuthorization(GuardianPermission Permissions, string AuthorizationSource);
