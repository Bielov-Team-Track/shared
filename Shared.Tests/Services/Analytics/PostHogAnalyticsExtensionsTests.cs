using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Shared.Services.Analytics;

namespace Shared.Tests.Services.Analytics;

[TestFixture]
[Category("Unit")]
public class PostHogAnalyticsExtensionsTests
{
    private static ServiceProvider Build(params (string Key, string Value)[] settings)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings.Select(s => new KeyValuePair<string, string?>(s.Key, s.Value)))
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IConfiguration>(configuration);
        services.AddPostHogAnalytics(configuration, "clubs-service");

        return services.BuildServiceProvider();
    }

    [Test]
    public void AddPostHogAnalytics_WithoutAnApiKey_RegistersTheNoOpAndNoSender()
    {
        // Act
        using var provider = Build();

        // Assert
        provider.GetRequiredService<IAnalyticsCapture>().Should().BeOfType<NoOpAnalyticsCapture>();
        provider.GetServices<IHostedService>().Should().NotContain(s => s is PostHogCaptureDispatcher);
        provider.GetService<AnalyticsCaptureQueue>().Should().BeNull();
    }

    [Test]
    public void AddPostHogAnalytics_WithoutAnApiKey_CapturesNothing()
    {
        // Arrange
        using var provider = Build();
        var capture = provider.GetRequiredService<IAnalyticsCapture>();

        // Act
        var act = () => capture.Capture(Guid.NewGuid(), AnalyticsEvents.ClubCreated);

        // Assert
        act.Should().NotThrow();
    }

    [Test]
    public void AddPostHogAnalytics_WithAnApiKey_RegistersTheQueueingCaptureAndItsDispatcher()
    {
        // Act
        using var provider = Build(("PostHog:ApiKey", "phc_test_key"));

        // Assert
        provider.GetRequiredService<IAnalyticsCapture>().Should().BeOfType<PostHogAnalyticsCapture>();
        provider.GetServices<IHostedService>().Should().ContainSingle(s => s is PostHogCaptureDispatcher);
        provider.GetRequiredService<IClientSourceAccessor>().Should().BeOfType<ClientSourceAccessor>();
    }

    [Test]
    public void AddPostHogAnalytics_WithAnApiKeyAndNoHost_FallsBackToTheEuIngest()
    {
        // Arrange
        using var provider = Build(("PostHog:ApiKey", "phc_test_key"));

        // Act
        var sender = provider.GetRequiredService<PostHogCaptureSender>();

        // Assert — construction validates the bound options; the default host must satisfy them
        sender.Should().NotBeNull();
    }

    [Test]
    public void AddPostHogAnalytics_WithAKeyAndAnInvalidHost_FailsOnFirstUseRatherThanSendingSomewhereElse()
    {
        // Arrange
        using var provider = Build(("PostHog:ApiKey", "phc_test_key"), ("PostHog:Host", "not-a-url"));

        // Act
        var resolve = () => provider.GetRequiredService<PostHogCaptureSender>();

        // Assert
        resolve.Should().Throw<OptionsValidationException>();
    }
}
