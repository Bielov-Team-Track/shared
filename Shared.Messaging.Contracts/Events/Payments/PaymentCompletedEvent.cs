namespace Shared.Messaging.Contracts.Events.Payments;

/// <summary>
/// Published by payments-service when a payment is completed (Stripe webhook or manual confirmation).
/// Events-service consumes this to update participant payment status.
/// </summary>
public record PaymentCompletedEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public required Guid PaymentId { get; init; }
    public required Guid EventParticipantId { get; init; }
    public required decimal Amount { get; init; }
    public required string Currency { get; init; }
    public string? PaymentStatus { get; init; } // "paid", "failed", "refunded" -- nullable for backward compatibility
}
