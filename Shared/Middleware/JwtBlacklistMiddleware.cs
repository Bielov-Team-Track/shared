using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Shared.Enums;
using Shared.Exceptions;

namespace Shared.Middleware;

public class JwtBlacklistMiddleware
{
    private readonly RequestDelegate _next;

    public JwtBlacklistMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IDistributedCache cache)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            // Check if all tokens for this user have been blacklisted (e.g., consent revocation)
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                var blacklisted = await cache.GetStringAsync($"jwt_blacklist:{userId}");
                if (blacklisted != null)
                {
                    throw new UnauthorizedException(
                        "Token has been revoked",
                        ErrorCodeEnum.TokenInvalid);
                }
            }
        }

        await _next(context);
    }
}
