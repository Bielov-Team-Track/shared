namespace Shared.Services.Analytics;

public sealed record CaptureEnvelope(
    string Event,
    string DistinctId,
    IReadOnlyDictionary<string, object?> Properties,
    DateTimeOffset Timestamp);
