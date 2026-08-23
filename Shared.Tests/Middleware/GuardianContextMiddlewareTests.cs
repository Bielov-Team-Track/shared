using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
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
    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly Guid SubjectId = Guid.NewGuid();

    private bool _nextRan;

    [SetUp]
    public void SetUp()
    {
        _nextRan = false;
    }

    private GuardianContextMiddleware Sut() => new(_ =>
    {
        _nextRan = true;
        return Task.CompletedTask;
    });

    private Task Invoke(HttpContext context) => Sut().InvokeAsync(context);

    private static DefaultHttpContext Context(string? actingAs = null, bool authenticated = true)
    {
        var context = new DefaultHttpContext();

        if (authenticated)
        {
            context.User = new ClaimsPrincipal(
                new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, ActorId.ToString())], "TestAuth"));
        }

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
        await Invoke(context);

        // Assert
        _nextRan.Should().BeTrue();
        context.Items.Should().BeEmpty();
    }

    [Test]
    public async Task HeaderAndMarkedEndpoint_PassesThrough()
    {
        // Arrange
        var context = Context(SubjectId.ToString());
        Mark(context);

        // Act
        await Invoke(context);

        // Assert
        _nextRan.Should().BeTrue();
        context.Items.Should().BeEmpty();
    }

    [Test]
    public async Task HeaderAndUnmarkedEndpoint_Throws400()
    {
        // Arrange
        var context = Context(SubjectId.ToString());
        LeaveUnmarked(context);

        // Act & Assert
        (await this.Invoking(_ => Invoke(context))
                .Should().ThrowAsync<BadRequestException>())
            .Which.Should().Match<BadRequestException>(e =>
                e.Message == "This endpoint does not accept X-Acting-As" &&
                e.ErrorCode == ErrorCodeEnum.ActingAsValidationFailed);
        _nextRan.Should().BeFalse();
    }

    /// <summary>A hub or a health route carries no endpoint metadata, and must be refused too.</summary>
    [Test]
    public async Task NoEndpointMetadata_Throws400()
    {
        // Arrange
        var context = Context(SubjectId.ToString());

        // Act & Assert
        await this.Invoking(_ => Invoke(context)).Should().ThrowAsync<BadRequestException>();
        _nextRan.Should().BeFalse();
    }

    /// <summary>
    /// Until 2b a real guardian link let an unmarked endpoint through, and the controller then read
    /// the ward out of HttpContext.Items. Nothing is written now, so nothing can read it.
    /// </summary>
    [Test]
    public async Task HeaderAndUnmarkedEndpoint_WritesNoContextItems()
    {
        // Arrange
        var context = Context(SubjectId.ToString());
        LeaveUnmarked(context);

        // Act
        await this.Invoking(_ => Invoke(context)).Should().ThrowAsync<BadRequestException>();

        // Assert
        context.Items.Should().BeEmpty();
    }

    /// <summary>The header is never parsed: a malformed value is refused for the same reason a valid one is.</summary>
    [Test]
    public async Task HeaderIsNotAGuid_ThrowsTheSameRefusal()
    {
        // Arrange
        var context = Context("not-a-guid");
        LeaveUnmarked(context);

        // Act & Assert
        (await this.Invoking(_ => Invoke(context))
                .Should().ThrowAsync<BadRequestException>())
            .Which.ErrorCode.Should().Be(ErrorCodeEnum.ActingAsValidationFailed);
    }

    /// <summary>
    /// auth-service registers this middleware in front of anonymous routes, so the refusal must not
    /// depend on there being a JWT to compare the header against.
    /// </summary>
    [Test]
    public async Task HeaderAndUnauthenticatedCaller_Throws400()
    {
        // Arrange
        var context = Context(SubjectId.ToString(), authenticated: false);
        LeaveUnmarked(context);

        // Act & Assert
        await this.Invoking(_ => Invoke(context)).Should().ThrowAsync<BadRequestException>();
        _nextRan.Should().BeFalse();
    }
}
