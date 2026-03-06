namespace Shared.Messaging.Contracts.Events.Family;

public class GuardianInvitationSentEvent : IEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public Guid InvitationId { get; set; }
    public Guid MinorUserId { get; set; }
    public string GuardianEmail { get; set; } = string.Empty;
    // Note: Token is NOT included here. Notification service fetches it via gRPC using InvitationId.
    public DateTime ExpiresAt { get; set; }
    public DateTime Timestamp { get; set; }
    public int Version { get; set; } = 1;
}
