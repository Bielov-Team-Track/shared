namespace Shared.Enums;

public enum ConsentType
{
    /// <summary>
    /// "No consent gate." A sentinel, never a grantable consent: attribute arguments cannot be a
    /// nullable enum, so [AcceptsSubject] needs a value that means the absence of one.
    /// </summary>
    None,
    CoreProfileData,
    EventParticipation,
    Messaging,
    PhotoStorage,
    PhotoPublicSharing,
    VideoStorage,
    VideoPublicSharing,
    PaymentProcessing,
    ThirdPartySharing,
    Geolocation,
    AITraining
}
