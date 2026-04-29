namespace Shared.Enums;

/// <summary>
/// How the payment was satisfied. Drives revenue reporting:
/// Card/Cash count as collected; Subscription is covered separately;
/// ManualAdjustment displays as "Other" and is excluded from collected totals.
/// </summary>
public enum PaymentMethodType
{
    Card = 0,
    Cash = 1,
    Subscription = 2,
    ManualAdjustment = 3
}
