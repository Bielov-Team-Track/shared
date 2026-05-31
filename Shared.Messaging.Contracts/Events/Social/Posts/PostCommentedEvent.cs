using Shared.Enums;

namespace Shared.Messaging.Contracts.Events.Social.Posts;

public class PostCommentedEvent : INotificationEvent
{
    public Guid UserId { get; init; }
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public Guid PostId { get; init; }
    public Guid CommentId { get; init; }
    public Guid CommentAuthorId { get; init; }
    public required string CommentAuthorName { get; init; }
    public ContextType ContextType { get; init; }
    public Guid ContextId { get; init; }
    public string? CommentPreview { get; init; }
}
