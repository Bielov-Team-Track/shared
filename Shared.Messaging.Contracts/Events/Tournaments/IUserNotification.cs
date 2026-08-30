namespace Shared.Messaging.Contracts.Events.Tournaments;

/// <summary>
/// A notification addressed to a set of people, carrying the words it should be shown in and the
/// page it opens. The publisher decides all three, so notifications-service can turn any of these
/// into a push and an inbox row without knowing what a tournament is.
/// </summary>
public interface IUserNotification : IEvent
{
    IReadOnlyList<Guid> RecipientUserIds { get; }
    string Title { get; }
    string Body { get; }

    /// Absolute web URL the notification opens.
    string DeepLink { get; }

    /// The delivery category, by the name notifications-service knows it as.
    string Category { get; }
}
