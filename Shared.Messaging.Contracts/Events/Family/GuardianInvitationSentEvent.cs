namespace Shared.Messaging.Contracts.Events.Family;

public class GuardianInvitationSentEvent : IEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public Guid InvitationId { get; set; }
    public Guid MinorUserId { get; set; }
    public string GuardianEmail { get; set; } = string.Empty;
    // The guardian usually has no account yet, so the emailed accept link is the only channel that
    // can carry the token. The outbox row lives in the same database as the token itself, so putting
    // it on the event adds no exposure a gRPC fetch would avoid. Never log it.
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime Timestamp { get; set; }
    public int Version { get; set; } = 1;
}
