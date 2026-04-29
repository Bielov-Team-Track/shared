namespace Shared.Enums;

/// <summary>
/// Canonical, user-facing payment status. Owned by payments-service; projected by other services.
/// Single source of truth for whether a payable target (e.g. an EventParticipant) is paid.
/// </summary>
public enum PaymentStatus
{
    Unpaid = 0,
    Pending = 1,
    Paid = 2,
    NotApplicable = 3,
    Failed = 4,
    Refunded = 5
}
