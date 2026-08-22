using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shared.Enums;
using Shared.Exceptions;
using Shared.Microservices.Guardian;
using Shared.Middleware;
using Shared.Services;

namespace Shared.Tests.Middleware;

[TestFixture]
[Category("Unit")]
public class GuardianContextMiddlewareTests
{
    private const string AuthSource = "cache";

    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly Guid SubjectId = Guid.NewGuid();

    private IGuardianCacheService _cache = null!;
    private IActionRiskClassifier _riskClassifier = null!;
    private bool _nextRan;

    [SetUp]
    public void SetUp()
    {
        _nextRan = false;
        _cache = Substitute.For<IGuardianCacheService>();
        _riskClassifier = Substitute.For<IActionRiskClassifier>();

        _riskClassifier.GetRiskLevel(Arg.Any<HttpRequest>()).Returns(ActionRiskLevel.Low);
        _cache.HasAccessWithCacheAsync(ActorId, SubjectId).Returns((true, AuthSource));
        _cache.HasAccessFromDbAsync(ActorId, SubjectId).Returns((true, AuthSource));
    }

    private GuardianContextMiddleware Sut() => new(_ =>
    {
        _nextRan = true;
        return Task.CompletedTask;
    });

    private Task Invoke(HttpContext context, bool rejectUnmarkedEndpoints = false) =>
        Sut().InvokeAsync(context, _cache, _riskClassifier,
            new OptionsWrapper<GuardianDelegationSettings>(
                new GuardianDelegationSettings { RejectUnmarkedEndpoints = rejectUnmarkedEndpoints }));

    private static DefaultHttpContext Context(string? actingAs = null)
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(
                new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, ActorId.ToString())], "TestAuth"))
        };

        if (actingAs is not null)
            context.Request.Headers[GuardianContextKeys.ActingAsHeader] = actingAs;

        return context;
    }

    private static void Mark(HttpContext context) =>
        context.SetEndpoint(new Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(new AcceptsSubjectAttribute(GuardianPermission.Register)),
            "marked"));

    private static void LeaveUnmarked(HttpContext context) =>
        context.SetEndpoint(new Endpoint(_ => Task.CompletedTask, EndpointMetadataCollection.Empty, "unmarked"));

    [Test]
    public async Task NoHeader_PassesThrough()
    {
        // Arrange
        var context = Context();
        LeaveUnmarked(context);

        // Act
        await Invoke(context, rejectUnmarkedEndpoints: true);

        // Assert
        _nextRan.Should().BeTrue();
        context.Items.Should().BeEmpty();
    }

    [Test]
    public async Task HeaderAndMarkedEndpoint_PassesThroughWithoutTouchingTheCache()
    {
        // Arrange
        var context = Context(SubjectId.ToString());
        Mark(context);

        // Act
        await Invoke(context, rejectUnmarkedEndpoints: true);

        // Assert
        _nextRan.Should().BeTrue();
        context.Items.Should().BeEmpty();
        await _cache.DidNotReceiveWithAnyArgs().HasAccessWithCacheAsync(default, default);
        await _cache.DidNotReceiveWithAnyArgs().HasAccessFromDbAsync(default, default);
    }

    [Test]
    public async Task HeaderAndUnmarkedEndpoint_RejectFlagOn_Throws400()
    {
        // Arrange
        var context = Context(SubjectId.ToString());
        LeaveUnmarked(context);

        // Act & Assert
        (await this.Invoking(_ => Invoke(context, rejectUnmarkedEndpoints: true))
                .Should().ThrowAsync<BadRequestException>())
            .Which.Should().Match<BadRequestException>(e =>
                e.Message == "This endpoint does not accept X-Acting-As" &&
                e.ErrorCode == ErrorCodeEnum.ActingAsValidationFailed);
        _nextRan.Should().BeFalse();
    }

    /// <summary>A hub or a health route carries no endpoint metadata, and must be refused too.</summary>
    [Test]
    public async Task NoEndpointMetadata_RejectFlagOn_Throws400()
    {
        // Arrange
        var context = Context(SubjectId.ToString());

        // Act & Assert
        await this.Invoking(_ => Invoke(context, rejectUnmarkedEndpoints: true))
            .Should().ThrowAsync<BadRequestException>();
        _nextRan.Should().BeFalse();
    }

    [Test]
    public async Task HeaderAndUnmarkedEndpoint_RejectFlagOff_RunsTodaysSevenSteps()
    {
        // Arrange
        var context = Context(SubjectId.ToString());
        LeaveUnmarked(context);

        // Act
        await Invoke(context);

        // Assert
        await _cache.Received(1).HasAccessWithCacheAsync(ActorId, SubjectId);
        await _cache.Received(1).GetRemovalNoticeStatusAsync(ActorId, SubjectId);
        _nextRan.Should().BeTrue();
    }

    [Test]
    public async Task HeaderAndUnmarkedEndpoint_RejectFlagOff_SetsActingAsUserId()
    {
        // Arrange
        var context = Context(SubjectId.ToString());
        LeaveUnmarked(context);

        // Act
        await Invoke(context);

        // Assert
        context.Items[GuardianContextKeys.LegacyActingAsUserId].Should().Be(SubjectId);
        context.Items[GuardianContextKeys.ActorUserId].Should().Be(ActorId);
        context.Items[GuardianContextKeys.AuthorizationSource].Should().Be(AuthSource);
        context.Items[GuardianContextKeys.Processed].Should().Be(true);
    }

    /// <summary>The compatibility branch must not write the key the filter owns.</summary>
    [Test]
    public async Task HeaderAndUnmarkedEndpoint_RejectFlagOff_DoesNotSetTheSubjectKey()
    {
        // Arrange
        var context = Context(SubjectId.ToString());
        LeaveUnmarked(context);

        // Act
        await Invoke(context);

        // Assert
        context.Items.Should().NotContainKey(GuardianContextKeys.SubjectUserId);
    }

    [Test]
    public async Task HeaderAndUnmarkedEndpoint_RejectFlagOffAndNoLink_ThrowsForbidden()
    {
        // Arrange
        var context = Context(SubjectId.ToString());
        LeaveUnmarked(context);
        _cache.HasAccessWithCacheAsync(ActorId, SubjectId).Returns((false, AuthSource));

        // Act & Assert
        await this.Invoking(_ => Invoke(context)).Should().ThrowAsync<ForbiddenException>();
        _nextRan.Should().BeFalse();
    }
}
