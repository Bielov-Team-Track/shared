namespace Shared.Messaging.Contracts.Events.Events;

public class EventPaymentReminderEvent : INotificationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public Guid UserId => RecipientUserId;

    public Guid RecipientUserId { get; set; }
    public Guid TargetEventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventStartTime { get; set; }
    public decimal Cost { get; set; }
    public string? PaymentMessage { get; set; }

    /// <summary>
    /// ISO code for <see cref="Cost"/>. Without it the amount cannot be shown at all — "8" could be
    /// pounds or euros — so the body says "Payment due" and leaves the figure out.
    /// </summary>
    public string? Currency { get; set; }
}
