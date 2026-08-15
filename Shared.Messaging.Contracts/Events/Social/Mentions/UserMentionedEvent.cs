using Shared.Enums.Social;

namespace Shared.Messaging.Contracts.Events.Social.Mentions;

public class UserMentionedEvent : INotificationEvent
{
    public Guid UserId { get; init; }
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public Guid MentionedByUserId { get; init; }
    public required string MentionedByUserName { get; set; }
    public Guid SourceId { get; init; }
    public MentionSourceType SourceType { get; init; }
    public Guid? PostId { get; init; }

    // Set when the mention resolved through a group (e.g. "Setters"); null for a personal
    // mention. notifications-service reads "X mentioned @Setters" only when this is set.
    public string? GroupLabel { get; init; }
}
