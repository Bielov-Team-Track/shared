using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Mime;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Shared.Enums;
using Shared.Exceptions;
using Shared.Extensions;

namespace Shared.Middleware;

[ExcludeFromCodeCoverage]
public class ErrorHandlerMiddleware
{
    private readonly RequestDelegate _next;

    public ErrorHandlerMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    protected virtual async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var (statusCode, response) = ex switch
        {
            ExceptionWithResult<object> => HandleExceptionWithResult(ex),
            ExceptionWithStatusAndErrorCodes => HandleExceptionWithStatusAndErrorCodes(ex, context),
            _ => (HttpStatusCode.InternalServerError,
                new BaseResponseWithErrorDetailsDto<object, object>(ErrorCodeEnum.InternalError, ex.Message))
        };

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = context.Request.Headers["Accept"] == MediaTypeNames.Application.Xml
            ? MediaTypeNames.Application.Xml
            : MediaTypeNames.Application.Json;

        var result = JsonSerializer.Serialize(response,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        await context.Response.WriteAsync(result);
    }

    private static (HttpStatusCode, BaseResponseWithErrorDetailsDto<object, object>) HandleExceptionWithStatusAndErrorCodes(Exception ex,
        HttpContext context)
    {
        var statusCode = HttpStatusCode.InternalServerError;

        // return base response with internal server error
        if (ex is not ExceptionWithStatusAndErrorCodes exception)
            return (statusCode, new BaseResponseWithErrorDetailsDto<object, object>(ErrorCodeEnum.InternalError, ex.Message));

        statusCode = exception.StatusCode;

        UnauthorizedStatusOverride(statusCode, exception.ErrorCode, context);

        return (statusCode, new BaseResponseWithErrorDetailsDto<object, object>(exception.ErrorCode, exception.Message, exception.ErrorDetails));
    }

    private static (HttpStatusCode, BaseResponseWithErrorDetailsDto<object, object>) HandleExceptionWithResult(Exception ex)
    {
        return ex is not ExceptionWithResult<object> exception
            ? (HttpStatusCode.InternalServerError, new BaseResponseWithErrorDetailsDto<object, object>(ErrorCodeEnum.InternalError, ex.Message))
            : (exception.StatusCode, new BaseResponseWithErrorDetailsDto<object, object>(exception.Result, exception.ErrorCode, exception.Message));
    }

    // please double-check as this code looks like not-working.
    // in case we need handle error in exceptional cases (like return 401 status in case if user logged in but not found) 
    private static void UnauthorizedStatusOverride(HttpStatusCode statusCode,
        ErrorCodeEnum errorCodeEnum,
        HttpContext context)
    {
        var errorsToOverride = new[]
        {
            ErrorCodeEnum.Unauthorized
        };

        if (errorsToOverride.Contains(errorCodeEnum) && context.IsUserLoggedIn())
            statusCode = HttpStatusCode.Unauthorized;
    }
}