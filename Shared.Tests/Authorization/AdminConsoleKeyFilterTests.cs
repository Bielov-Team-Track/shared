using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Shared.Microservices.Authorization;

namespace Shared.Tests.Authorization;

[TestFixture]
[Category("Unit")]
public class AdminConsoleKeyFilterTests
{
    private const string ConfiguredKey = "the-console-secret";

    [Test]
    public void OnAuthorization_WithMatchingKey_LetsTheRequestThrough()
    {
        var context = ContextWithHeader(ConfiguredKey);

        FilterFor(ConfiguredKey).OnAuthorization(context);

        Assert.That(context.Result, Is.Null);
    }

    [Test]
    public void OnAuthorization_WithWrongKey_Refuses()
    {
        var context = ContextWithHeader("not-the-console-secret");

        FilterFor(ConfiguredKey).OnAuthorization(context);

        Assert.That(context.Result, Is.TypeOf<UnauthorizedResult>());
    }

    [Test]
    public void OnAuthorization_WithNoHeader_Refuses()
    {
        var context = ContextWithHeader(null);

        FilterFor(ConfiguredKey).OnAuthorization(context);

        Assert.That(context.Result, Is.TypeOf<UnauthorizedResult>());
    }

    /// <summary>
    /// The case a naive equality check gets wrong: an unset secret matching an absent header.
    /// </summary>
    [TestCase(null)]
    [TestCase("")]
    public void OnAuthorization_WhenTheSecretIsUnconfigured_RefusesEvenAnEmptyHeader(string? configured)
    {
        var context = ContextWithHeader(configured);

        FilterFor(configured).OnAuthorization(context);

        Assert.That(context.Result, Is.TypeOf<UnauthorizedResult>());
    }

    /// <summary>
    /// A prefix of the real key must fare no better than nonsense — the comparison is over
    /// digests, so length and leading bytes tell an attacker nothing.
    /// </summary>
    [Test]
    public void OnAuthorization_WithAPrefixOfTheKey_Refuses()
    {
        var context = ContextWithHeader(ConfiguredKey[..^1]);

        FilterFor(ConfiguredKey).OnAuthorization(context);

        Assert.That(context.Result, Is.TypeOf<UnauthorizedResult>());
    }

    [Test]
    public void OnAuthorization_WithADifferentlyCasedKey_Refuses()
    {
        var context = ContextWithHeader(ConfiguredKey.ToUpperInvariant());

        FilterFor(ConfiguredKey).OnAuthorization(context);

        Assert.That(context.Result, Is.TypeOf<UnauthorizedResult>());
    }

    private static AdminConsoleKeyFilter FilterFor(string? apiKey) =>
        new(new OptionsWrapper<AdminConsoleSettings>(new AdminConsoleSettings { ApiKey = apiKey ?? string.Empty }));

    private static AuthorizationFilterContext ContextWithHeader(string? value)
    {
        var httpContext = new DefaultHttpContext();
        if (value is not null)
        {
            httpContext.Request.Headers[AdminConsoleKeyFilter.HeaderName] = value;
        }

        return new AuthorizationFilterContext(
            new ActionContext(httpContext, new RouteData(), new ActionDescriptor()),
            []);
    }
}
