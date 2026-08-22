namespace Shared.Services;

/// <summary>
/// The HttpContext.Items slots the acting-as machinery writes and the controllers read.
/// </summary>
public static class GuardianContextKeys
{
    /// <summary>
    /// Whose data the request is about. Deliberately NOT "ActingAsUserId": during the 2a window
    /// the middleware's compatibility branch writes that key and the filter writes this one, so
    /// nothing can read a legacy value where a validated subject is meant.
    /// </summary>
    public const string SubjectUserId = "SubjectUserId";

    /// <summary>
    /// Who is making the request. The string stays "ActualUserId" so BaseApiController.ActualUserId
    /// keeps reading the same slot on both paths.
    /// </summary>
    public const string ActorUserId = "ActualUserId";

    public const string AuthorizationSource = "AuthorizationSource";
    public const string Processed = "GuardianContextProcessed";
    public const string ActingAsHeader = "X-Acting-As";

    /// <summary>2a only. Deleted in 2b together with EffectiveUserId.</summary>
    public const string LegacyActingAsUserId = "ActingAsUserId";
}
