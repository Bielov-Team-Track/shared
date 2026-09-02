using System.Threading.Channels;

namespace Shared.Services.Analytics;

/// <summary>
/// The hand-off between the request thread and the sender. Bounded, and events are dropped
/// rather than queued without limit: a PostHog outage must cost analytics and nothing else.
/// </summary>
public sealed class AnalyticsCaptureQueue
{
    public const int Capacity = 1000;

    // Wait is the only full-mode whose TryWrite reports the drop instead of silently swallowing
    // it. TryWrite itself never blocks, so the request thread walks away either way.
    private readonly Channel<CaptureEnvelope> _channel = Channel.CreateBounded<CaptureEnvelope>(
        new BoundedChannelOptions(Capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });

    private int _dropped;

    public bool TryEnqueue(CaptureEnvelope envelope)
    {
        if (_channel.Writer.TryWrite(envelope))
            return true;

        Interlocked.Increment(ref _dropped);
        return false;
    }

    public ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken) =>
        _channel.Reader.WaitToReadAsync(cancellationToken);

    public List<CaptureEnvelope> DrainUpTo(int maxCount)
    {
        var batch = new List<CaptureEnvelope>();
        while (batch.Count < maxCount && _channel.Reader.TryRead(out var envelope))
            batch.Add(envelope);

        return batch;
    }

    /// <summary>
    /// Reads and clears the drop count, so the sender reports drops once per pass rather than
    /// logging from the request thread once per lost event.
    /// </summary>
    public int TakeDroppedCount() => Interlocked.Exchange(ref _dropped, 0);
}
