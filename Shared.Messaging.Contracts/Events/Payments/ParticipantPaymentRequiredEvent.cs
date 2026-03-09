namespace Shared.Messaging.Contracts.Events.Payments;

/// <summary>
/// Published by events-service when a participant joins a paid event and needs a payment record created.
/// </summary>
public record ParticipantPaymentRequiredEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public required Guid EventParticipantId { get; init; }
    public Guid? UserId { get; init; }       // For Payment.UserId denormalization
    public Guid? SourceEventId { get; init; } // For Payment.EventId denormalization
    public required decimal Amount { get; init; }
    public required string Currency { get; init; }
    public required string PaymentProvider { get; init; }
}
