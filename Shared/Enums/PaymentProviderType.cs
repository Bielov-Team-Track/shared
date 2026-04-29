namespace Shared.Enums;

/// <summary>
/// Which system originated the payment record.
/// Stripe for card flows, Internal for subscription cover, Manual for admin adjustments.
/// </summary>
public enum PaymentProviderType
{
    Stripe = 0,
    Internal = 1,
    Manual = 2
}
