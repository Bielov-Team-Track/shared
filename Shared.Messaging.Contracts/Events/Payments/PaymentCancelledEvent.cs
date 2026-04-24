using System.Text.Json;

namespace Shared.Messaging.Contracts.Events.Payments;

/// <summary>
/// Published by payments-service when a payment is cancelled — either by explicit caller request
/// (user closed modal, reservation timed out) or Stripe reported the PI as canceled.
/// </summary>
public record PaymentCancelledEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public required Guid PaymentId { get; init; }
    public required string PaymentKey { get; init; }
    public required string TargetType { get; init; }
    public required Guid TargetId { get; init; }
    public required JsonDocument Metadata { get; init; }
    public required string Reason { get; init; }
}
