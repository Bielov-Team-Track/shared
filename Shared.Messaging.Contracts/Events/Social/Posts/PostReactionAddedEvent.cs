using Shared.Enums;

namespace Shared.Messaging.Contracts.Events.Social.Posts;

public class PostReactionAddedEvent : INotificationEvent
{
    /// <summary>The recipient: the post's author, or the comment's author when <see cref="CommentId"/> is set.</summary>
    public Guid UserId { get; init; }
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public Guid PostId { get; init; }
    /// <summary>Set when the reaction landed on a comment rather than the post itself.</summary>
    public Guid? CommentId { get; init; }
    public Guid ReactorId { get; init; }
    public required string ReactorName { get; init; }
    public required string Emoji { get; init; }
    public ContextType ContextType { get; init; }
    public Guid ContextId { get; init; }
    /// <summary>Preview of the reacted-to content, so the notification says which post/comment.</summary>
    public string? TargetPreview { get; init; }
}
