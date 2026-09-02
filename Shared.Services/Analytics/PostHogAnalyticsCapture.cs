using System.Text;

namespace Shared.Services.Analytics;

public sealed class PostHogAnalyticsCapture : IAnalyticsCapture
{
    private const string SourceProperty = "source";
    private const string ClientSourceProperty = "client_source";
    private const string ServiceProperty = "service";
    private const string ServerSource = "server";

    private readonly AnalyticsCaptureQueue _queue;
    private readonly IClientSourceAccessor _clientSourceAccessor;
    private readonly TimeProvider _timeProvider;
    private readonly string _serviceName;

    public PostHogAnalyticsCapture(
        AnalyticsCaptureQueue queue,
        IClientSourceAccessor clientSourceAccessor,
        TimeProvider timeProvider,
        string serviceName)
    {
        _queue = queue;
        _clientSourceAccessor = clientSourceAccessor;
        _timeProvider = timeProvider;
        _serviceName = serviceName;
    }

    public void Capture(Guid userId, string eventName, IReadOnlyDictionary<string, object?>? properties = null)
    {
        var payload = new Dictionary<string, object?>((properties?.Count ?? 0) + 3);

        if (properties != null)
        {
            foreach (var (key, value) in properties)
                payload[key] = Normalize(value);
        }

        // Written last: these three belong to the transport, and no call site may shadow them.
        payload[SourceProperty] = ServerSource;
        payload[ClientSourceProperty] = _clientSourceAccessor.Current;
        payload[ServiceProperty] = _serviceName;

        _queue.TryEnqueue(new CaptureEnvelope(
            eventName, userId.ToString(), payload, _timeProvider.GetUtcNow()));
    }

    /// <summary>
    /// Enums travel as lower_snake names. Their numeric values have been reordered before now,
    /// and System.Text.Json would otherwise write the number.
    /// </summary>
    private static object? Normalize(object? value) =>
        value is Enum enumValue ? ToSnakeCase(enumValue.ToString()) : value;

    private static string ToSnakeCase(string name)
    {
        var builder = new StringBuilder(name.Length + 4);

        for (var i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]))
                builder.Append('_');

            builder.Append(char.ToLowerInvariant(name[i]));
        }

        return builder.ToString();
    }
}
