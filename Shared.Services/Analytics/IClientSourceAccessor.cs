namespace Shared.Services.Analytics;

public interface IClientSourceAccessor
{
    /// <summary>
    /// The client behind the request being served, or <see cref="ClientSource.Unknown"/> when
    /// there is no request (a consumer, a background job) or the header was not sent.
    /// </summary>
    string Current { get; }
}
