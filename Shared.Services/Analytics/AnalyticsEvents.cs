namespace Shared.Services.Analytics;

/// <summary>
/// The server-owned event names from docs/analytics/events.md. A shipped name is never renamed
/// or repurposed — PostHog insights are built on the string — so this list only ever grows.
/// </summary>
public static class AnalyticsEvents
{
    public const string AccountCreated = "account_created";
    public const string EmailVerified = "email_verified";
    public const string ClubCreated = "club_created";
    public const string InvitationSent = "invitation_sent";
    public const string InvitationAccepted = "invitation_accepted";
}
