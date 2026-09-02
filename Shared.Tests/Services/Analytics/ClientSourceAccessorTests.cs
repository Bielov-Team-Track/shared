using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Shared.Services.Analytics;

namespace Shared.Tests.Services.Analytics;

[TestFixture]
[Category("Unit")]
public class ClientSourceAccessorTests
{
    private static ClientSourceAccessor MakeSut(HttpContext? context)
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(context);
        return new ClientSourceAccessor(accessor);
    }

    private static HttpContext RequestWith(string headerValue)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[ClientSource.HeaderName] = headerValue;
        return context;
    }

    [TestCase("web", ClientSource.Web)]
    [TestCase("ios", ClientSource.Ios)]
    [TestCase("android", ClientSource.Android)]
    [TestCase("Android", ClientSource.Android)]
    [TestCase("  WEB  ", ClientSource.Web)]
    public void Current_WithAKnownClient_ReturnsTheNormalizedName(string header, string expected)
    {
        // Act
        var current = MakeSut(RequestWith(header)).Current;

        // Assert
        current.Should().Be(expected);
    }

    [TestCase("desktop")]
    [TestCase("")]
    public void Current_WithAnUnrecognisedHeader_ReturnsUnknownRatherThanGuessing(string header)
    {
        // Act
        var current = MakeSut(RequestWith(header)).Current;

        // Assert
        current.Should().Be(ClientSource.Unknown);
    }

    [Test]
    public void Current_WithoutTheHeader_ReturnsUnknown()
    {
        // Act
        var current = MakeSut(new DefaultHttpContext()).Current;

        // Assert
        current.Should().Be(ClientSource.Unknown);
    }

    [Test]
    public void Current_OutsideARequest_ReturnsUnknown()
    {
        // Act
        var current = MakeSut(null).Current;

        // Assert
        current.Should().Be(ClientSource.Unknown);
    }
}
