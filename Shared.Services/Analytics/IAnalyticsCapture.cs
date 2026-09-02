namespace Shared.Services.Analytics;

public interface IAnalyticsCapture
{
    /// <summary>
    /// Records a server-owned fact against a user. Returns as soon as the event is queued —
    /// the caller never waits on PostHog — so this belongs strictly <em>after</em> the
    /// SaveChangesAsync that made the fact true, never inside the transaction.
    /// </summary>
    /// <param name="properties">
    /// Ids, enums, counts and booleans only. Never names, emails, phone numbers, dates of
    /// birth or free text: server events are kept under legitimate interest, not consent.
    /// </param>
    void Capture(Guid userId, string eventName, IReadOnlyDictionary<string, object?>? properties = null);
}
