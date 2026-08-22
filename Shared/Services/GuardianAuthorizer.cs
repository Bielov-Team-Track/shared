using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Shared.Enums;
using Shared.Exceptions;

namespace Shared.Services;

public class GuardianAuthorizer : IGuardianAuthorizer
{
    /*
     * One message for every refusal that turns on whether the pair exists, and no error code
     * on it. A refusal that explains itself tells a prober which half of the pair is real.
     */
    private const string AccessDenied = "Access denied";

    private readonly IGuardianCacheService _cache;
    private readonly IActionRiskClassifier _riskClassifier;
    private readonly IGuardianAccessSource _source;
    private readonly ILogger<GuardianAuthorizer> _logger;

    public GuardianAuthorizer(
        IGuardianCacheService cache,
        IActionRiskClassifier riskClassifier,
        IGuardianAccessSource source,
        ILogger<GuardianAuthorizer> logger)
    {
        _cache = cache;
        _riskClassifier = riskClassifier;
        _source = source;
        _logger = logger;
    }

    public async Task<GuardianAuthorization> AuthorizeAsync(
        Guid actorUserId,
        Guid subjectUserId,
        GuardianPermission requiredPermission,
        ConsentType? requiredConsent,
        HttpRequest request,
        CancellationToken ct = default)
    {
        var riskLevel = _riskClassifier.GetRiskLevel(request);

        var (hasAccess, authSource) = riskLevel >= ActionRiskLevel.High
            ? await _cache.HasAccessFromDbAsync(actorUserId, subjectUserId)
            : await _cache.HasAccessWithCacheAsync(actorUserId, subjectUserId);

        if (!hasAccess)
            throw new ForbiddenException(AccessDenied);

        var removalStatus = await _cache.GetRemovalNoticeStatusAsync(actorUserId, subjectUserId);
        if (removalStatus?.IsUnderRemovalNotice == true &&
            GuardianRequestClassification.IsWriteAction(request))
            throw new ForbiddenException(AccessDenied);

        // An endpoint that declares neither a permission nor a consent has already been answered
        // by the relationship check above, and must not pay for a second round trip.
        if (requiredPermission == GuardianPermission.None && requiredConsent is null)
            return new GuardianAuthorization(GuardianPermission.None, authSource);

        IReadOnlyCollection<ConsentType>? consents =
            requiredConsent is { } required ? [required] : null;

        var snapshot = await _source.CheckAsync(actorUserId, subjectUserId, consents, ct);

        if (snapshot is null)
        {
            /*
             * Fail closed, and do NOT fall back to the relationship answer above: that answer
             * carries no permission bits and no consents, so accepting it here would make every
             * declared gate decorative on exactly the days profiles-service is unwell.
             */
            _logger.LogWarning(
                "Guardian permission check unavailable for {ActorUserId} acting for {SubjectUserId}; denying",
                actorUserId, subjectUserId);
            throw new ForbiddenException(AccessDenied);
        }

        if (!snapshot.HasAccess || snapshot.IsUnderRemovalNotice)
            throw new ForbiddenException("Guardian does not have access to this minor",
                ErrorCodeEnum.GuardianAccessDenied);

        if (requiredPermission != GuardianPermission.None &&
            !snapshot.Permissions.HasFlag(requiredPermission))
            throw new ForbiddenException($"Guardian lacks {requiredPermission} permission",
                ErrorCodeEnum.GuardianAccessDenied);

        if (requiredConsent is { } consent && !snapshot.GrantedConsentTypes.Contains(consent))
            throw new ForbiddenException($"Minor has not consented to {consent}",
                ErrorCodeEnum.ConsentNotFound);

        return new GuardianAuthorization(snapshot.Permissions, authSource);
    }
}
