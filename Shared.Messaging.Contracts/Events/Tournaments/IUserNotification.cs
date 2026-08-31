namespace Shared.Messaging.Contracts.Events.Tournaments;

/// <summary>
/// A notification addressed to a set of people, carrying the words it should be shown in and the
/// page it opens. The publisher decides all three, so notifications-service can turn any of these
/// into a push and an inbox row without knowing what a tournament is.
/// </summary>
public interface IUserNotification : IEvent
{
    IReadOnlyList<Guid> RecipientUserIds { get; }

    /// <summary>
    /// Addresses with nobody behind them. A tournament is the one place a stranger takes part
    /// without an account — an emailed invitation, and the guest squad that accepts it — so some
    /// of these notifications are addressed to a person the platform cannot push to.
    /// </summary>
    IReadOnlyList<string> RecipientEmails { get; }

    string Title { get; }
    string Body { get; }

    /// Absolute web URL the notification opens.
    string DeepLink { get; }

    /// The delivery category, by the name notifications-service knows it as.
    string Category { get; }
}
