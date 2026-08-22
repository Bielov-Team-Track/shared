namespace Shared.Services;

public class GuardianDelegationSettings
{
    public const string SectionName = "GuardianDelegation";

    /// <summary>
    /// Refuse X-Acting-As on any endpoint that has not opted in with [AcceptsSubject].
    /// 2a: opt-in per service, so a service flips it in the same commit that marks its last
    /// endpoint. 2b: the setting is deleted and the rejection is unconditional.
    /// </summary>
    public bool RejectUnmarkedEndpoints { get; set; }
}
