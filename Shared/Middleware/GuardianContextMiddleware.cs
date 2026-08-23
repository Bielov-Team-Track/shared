using Microsoft.AspNetCore.Http;
using Shared.Enums;
using Shared.Exceptions;
using Shared.Services;

namespace Shared.Middleware;

/// <summary>
/// Refuses X-Acting-As on every endpoint that has not opted in with [AcceptsSubject].
/// </summary>
/// <remarks>
/// The refusal lives here rather than in the filter because a filter attached by an attribute
/// cannot run on an endpoint that lacks the attribute — and an endpoint that lacks it is exactly
/// the case that used to answer 200 having written to the actor's own account. A marked endpoint
/// is the filter's job end to end; the middleware never inspects the header's value.
/// </remarks>
public class GuardianContextMiddleware
{
    private readonly RequestDelegate _next;

    public GuardianContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.ContainsKey(GuardianContextKeys.ActingAsHeader))
        {
            await _next(context);
            return;
        }

        if (context.GetEndpoint()?.Metadata.GetMetadata<IAcceptsSubjectMetadata>() is null)
        {
            throw new BadRequestException(
                "This endpoint does not accept X-Acting-As",
                ErrorCodeEnum.ActingAsValidationFailed);
        }

        await _next(context);
    }
}
