using Shared.Enums;

namespace Shared.Messaging.Contracts.Events.Payments;

/// <summary>
/// Canonical signal for any change to a Payment's status. Published by payments-service through
/// the outbox after a durable status transition. Replaces the per-transition events
/// (PaymentCompletedEvent, PaymentFailedEvent, PaymentCancelledEvent) which remain only for
/// backwards-compat during migration.
///
/// Consumers must apply this event idempotently using <see cref="Version"/> — discard any event
/// whose Version is less than or equal to the projection's last-applied version.
/// </summary>
public record PaymentStatusChangedEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public required Guid PaymentId { get; init; }
    public required string TargetType { get; init; }
    public required Guid TargetId { get; init; }

    public required PaymentStatus OldStatus { get; init; }
    public required PaymentStatus NewStatus { get; init; }

    /// <summary>
    /// Monotonically-increasing version of the canonical Payment row.
    /// Projections must ignore events where Version &lt;= projection's last-applied version.
    /// </summary>
    public required long Version { get; init; }

    public required PaymentMethodType PaymentMethod { get; init; }
    public required PaymentProviderType Provider { get; init; }

    public Guid? ChangedByUserId { get; init; }
    public string? Reason { get; init; }
    public required DateTime ChangedAt { get; init; }
}
