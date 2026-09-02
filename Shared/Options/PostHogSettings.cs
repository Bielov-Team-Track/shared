namespace Shared.Options;

public class PostHogSettings
{
    public const string SectionName = "PostHog";

    public string Host { get; set; } = "https://eu.i.posthog.com";

    /// <summary>
    /// Absent everywhere but the deployed environments. No key means no capture at all,
    /// so a developer needs no PostHog project to run the stack.
    /// </summary>
    public string? ApiKey { get; set; }
}
