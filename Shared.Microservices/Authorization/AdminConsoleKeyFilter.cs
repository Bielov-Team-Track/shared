using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace Shared.Microservices.Authorization;

/// <summary>
/// Refuses any request that does not carry the admin console's shared secret.
/// </summary>
/// <remarks>
/// One implementation for every service on purpose. A per-service copy of a constant-time
/// comparison is how one of them quietly becomes a byte-by-byte one.
/// </remarks>
public sealed class AdminConsoleKeyFilter : IAuthorizationFilter
{
    public const string HeaderName = "X-Admin-Console-Key";

    private readonly AdminConsoleSettings _settings;

    public AdminConsoleKeyFilter(IOptions<AdminConsoleSettings> settings)
    {
        _settings = settings.Value;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        /*
         * Fails closed when unconfigured. A deployment that forgets the secret must refuse
         * every request rather than accept an empty key, which is what a naive equality
         * check against an unset setting would do.
         */
        if (string.IsNullOrEmpty(_settings.ApiKey))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var presented = context.HttpContext.Request.Headers[HeaderName].ToString();

        /*
         * Compared as hashes rather than as the raw strings. FixedTimeEquals returns false
         * immediately when the lengths differ, so comparing the keys directly would leak the
         * key's length; digests are always the same width. The fixed-time comparison itself
         * matters because a byte-by-byte one leaks the prefix to anyone able to time
         * responses, and the gateway makes these endpoints reachable from the internet.
         */
        var expected = SHA256.HashData(Encoding.UTF8.GetBytes(_settings.ApiKey));
        var actual = SHA256.HashData(Encoding.UTF8.GetBytes(presented));

        if (!CryptographicOperations.FixedTimeEquals(expected, actual))
        {
            /* No body. A refusal that explains itself tells a prober what to try next. */
            context.Result = new UnauthorizedResult();
        }
    }
}
