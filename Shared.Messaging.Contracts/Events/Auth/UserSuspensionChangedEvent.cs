namespace Shared.Messaging.Contracts.Events.Auth;

/// <summary>
/// auth owns whether an account is permitted to act. Published when an operator suspends or
/// restores one, so profiles and the services replicating <c>UserProfile</c> can hide or restore
/// it without each deciding for itself what a suspension means.
/// </summary>
/// <remarks>
/// The operator's reason is deliberately absent. It is investigation material, it lives in the
/// admin console's audit log, and there is no reason for it to be replicated into nine databases
/// to hide a row.
/// </remarks>
public record UserSuspensionChangedEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public required Guid UserId { get; init; }
    public required bool IsSuspended { get; init; }
}
