using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Shared.Enums;
using Shared.Exceptions;
using Shared.Services;

namespace Shared.Middleware;

/// <summary>
/// Refuses X-Acting-As on every endpoint that has not opted in with [AcceptsSubject].
/// </summary>
/// <remarks>
/// The refusal lives here rather than in the filter because a filter attached by an attribute
/// cannot run on an endpoint that lacks the attribute — and an endpoint that lacks it is exactly
/// the case that used to answer 200 having written to the actor's own account.
/// </remarks>
public class GuardianContextMiddleware
{
    private readonly RequestDelegate _next;

    public GuardianContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IGuardianCacheService guardianService,
        IActionRiskClassifier actionClassifier,
        IOptions<GuardianDelegationSettings> delegationSettings)
    {
        if (!context.Request.Headers.TryGetValue(GuardianContextKeys.ActingAsHeader, out var rawValue))
        {
            await _next(context);
            return;
        }

        // A marked endpoint is the filter's job end to end. Running the checks here as well would
        // double every acting-as request's cache reads and its constant-time delay.
        if (context.GetEndpoint()?.Metadata.GetMetadata<IAcceptsSubjectMetadata>() is not null)
        {
            await _next(context);
            return;
        }

        if (delegationSettings.Value.RejectUnmarkedEndpoints)
            throw new BadRequestException(
                "This endpoint does not accept X-Acting-As",
                ErrorCodeEnum.ActingAsValidationFailed);

        await RunLegacyGuardianContextAsync(context, guardianService, actionClassifier, rawValue.ToString());

        await _next(context);
    }

    /// <summary>
    /// The pre-[AcceptsSubject] path, kept working while the services migrate. Deleted in shared 2b
    /// together with EffectiveUserId.
    /// </summary>
    private static async Task RunLegacyGuardianContextAsync(
        HttpContext context,
        IGuardianCacheService guardianService,
        IActionRiskClassifier actionClassifier,
        string rawValue)
    {
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirst("nameid");

        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var jwtUserId))
            throw new UnauthorizedException(
                "Authentication required",
                ErrorCodeEnum.Unauthorized);

        if (!Guid.TryParse(rawValue, out var minorId))
            throw new BadRequestException(
                "X-Acting-As must be a valid GUID",
                ErrorCodeEnum.ActingAsValidationFailed);

        if (minorId == jwtUserId)
            throw new BadRequestException(
                "Cannot act as yourself",
                ErrorCodeEnum.ActingAsValidationFailed);

        // Constant-time against enumerating who is a minor.
        await Task.Delay(100);

        var riskLevel = actionClassifier.GetRiskLevel(context.Request);

        var (hasAccess, authSource) = riskLevel >= ActionRiskLevel.High
            ? await guardianService.HasAccessFromDbAsync(jwtUserId, minorId)
            : await guardianService.HasAccessWithCacheAsync(jwtUserId, minorId);

        if (!hasAccess)
            throw new ForbiddenException("Access denied");

        var removalStatus = await guardianService.GetRemovalNoticeStatusAsync(jwtUserId, minorId);
        if (removalStatus?.IsUnderRemovalNotice == true &&
            GuardianRequestClassification.IsWriteAction(context.Request))
            throw new ForbiddenException("Access denied");

        context.Items[GuardianContextKeys.LegacyActingAsUserId] = minorId;
        context.Items[GuardianContextKeys.ActorUserId] = jwtUserId;
        context.Items[GuardianContextKeys.AuthorizationSource] = authSource;
        context.Items[GuardianContextKeys.Processed] = true;
    }
}
