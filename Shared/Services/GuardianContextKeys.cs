namespace Shared.Services;

/// <summary>
/// The HttpContext.Items slots the acting-as machinery writes and the controllers read.
/// </summary>
public static class GuardianContextKeys
{
    /// <summary>
    /// Whose data the request is about. Written only by AcceptsSubjectFilter, so its presence is
    /// itself the proof that the subject was authorized.
    /// </summary>
    public const string SubjectUserId = "SubjectUserId";

    /// <summary>
    /// Who is making the request. BaseApiController.ActualUserId reads this slot.
    /// </summary>
    public const string ActorUserId = "ActualUserId";

    public const string AuthorizationSource = "AuthorizationSource";
    public const string Processed = "GuardianContextProcessed";
    public const string ActingAsHeader = "X-Acting-As";
}
