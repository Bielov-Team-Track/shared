using System.Text.Json;

namespace Shared.Messaging.Contracts.Events.Payments;

/// <summary>
/// Published by payments-service when a payment completes successfully (Stripe webhook
/// payment_intent.succeeded, manual confirmation, or subscription-covered instant payment).
/// Target services filter by TargetType and react.
/// </summary>
public record PaymentCompletedEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public required Guid PaymentId { get; init; }
    public required string PaymentKey { get; init; }
    public required string TargetType { get; init; }
    public required Guid TargetId { get; init; }
    public required JsonDocument Metadata { get; init; }
    public required DateTime CompletedAt { get; init; }
}
