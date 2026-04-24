using System.Text.Json;

namespace Shared.Messaging.Contracts.Events.Payments;

/// <summary>
/// Published by payments-service when a Stripe PaymentIntent fails terminally (charge declined, fraud rule, etc.).
/// Target services filter by TargetType and react (release reservations, notify user, etc.).
/// </summary>
public record PaymentFailedEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public required Guid PaymentId { get; init; }
    public required string PaymentKey { get; init; }
    public required string TargetType { get; init; }
    public required Guid TargetId { get; init; }
    public required JsonDocument Metadata { get; init; }
    public required string FailureCode { get; init; }
    public required string FailureMessage { get; init; }
}
