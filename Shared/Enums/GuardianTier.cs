namespace Shared.Enums;

/// <summary>
/// What kind of adult this is to the child. Single-valued on purpose: it says who somebody is,
/// while <see cref="GuardianPermission"/> — a flags mask — says what they may do. The tier's job
/// in code is to CAP the mask (see GuardianTierPermissions), so a Contact cannot hold Message and
/// therefore cannot observe a chat, without any surface needing to know the word "Contact".
/// Guardian is 0 so that a row, an event or a replica written before this enum existed reads back
/// as a full guardian, which is what every one of them is.
/// </summary>
public enum GuardianTier
{
    Guardian = 0,
    Contact = 1,
    Payer = 2
}
