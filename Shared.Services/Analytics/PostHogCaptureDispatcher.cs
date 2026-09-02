using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Shared.Services.Analytics;

/// <summary>
/// Drains the capture queue off the request path. Everything a request thread does is enqueue,
/// so PostHog's latency and availability never reach a user.
/// </summary>
public sealed class PostHogCaptureDispatcher : BackgroundService
{
    private const int MaxBatchSize = 50;

    private readonly AnalyticsCaptureQueue _queue;
    private readonly PostHogCaptureSender _sender;
    private readonly ILogger<PostHogCaptureDispatcher> _logger;

    public PostHogCaptureDispatcher(
        AnalyticsCaptureQueue queue,
        PostHogCaptureSender sender,
        ILogger<PostHogCaptureDispatcher> logger)
    {
        _queue = queue;
        _sender = sender;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (await _queue.WaitToReadAsync(stoppingToken))
            {
                var batch = _queue.DrainUpTo(MaxBatchSize);
                var failed = 0;

                // Sequential on purpose: a backlog is already late, and firing fifty requests at
                // once would answer an outage by leaning on it harder.
                foreach (var envelope in batch)
                {
                    if (!await _sender.SendAsync(envelope, stoppingToken))
                        failed++;
                }

                ReportLosses(failed, batch.Count);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private void ReportLosses(int failed, int batchSize)
    {
        var dropped = _queue.TakeDroppedCount();
        if (failed == 0 && dropped == 0)
            return;

        _logger.LogWarning(
            "PostHog capture lost events: {Failed} of {BatchSize} failed to send, {Dropped} dropped by a full queue",
            failed, batchSize, dropped);
    }
}
