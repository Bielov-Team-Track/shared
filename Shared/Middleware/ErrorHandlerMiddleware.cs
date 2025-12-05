using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Mime;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Shared.Enums;
using Shared.Exceptions;
using Shared.Extensions;

namespace Shared.Middleware;

[ExcludeFromCodeCoverage]
public class ErrorHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlerMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ErrorHandlerMiddleware(
        RequestDelegate next,
        ILogger<ErrorHandlerMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
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
        // Log the exception
        var requestPath = context.Request.Path;
        var requestMethod = context.Request.Method;

        var (statusCode, response) = ex switch
        {
            ExceptionWithResult<object> => HandleExceptionWithResult(ex),
            ExceptionWithStatusAndErrorCodes exWithCodes => HandleExceptionWithStatusAndErrorCodes(exWithCodes, context),
            _ => HandleUnexpectedException(ex)
        };

        // Log based on status code severity
        if (statusCode >= HttpStatusCode.InternalServerError)
        {
            _logger.LogError(ex,
                "Unhandled exception occurred. Method: {Method}, Path: {Path}, StatusCode: {StatusCode}, Message: {Message}",
                requestMethod, requestPath, (int)statusCode, ex.Message);
        }
        else if (statusCode >= HttpStatusCode.BadRequest)
        {
            _logger.LogWarning(
                "Client error occurred. Method: {Method}, Path: {Path}, StatusCode: {StatusCode}, Message: {Message}",
                requestMethod, requestPath, (int)statusCode, ex.Message);
        }

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = context.Request.Headers["Accept"] == MediaTypeNames.Application.Xml
            ? MediaTypeNames.Application.Xml
            : MediaTypeNames.Application.Json;

        var result = JsonSerializer.Serialize(response,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        await context.Response.WriteAsync(result);
    }

    private (HttpStatusCode, BaseResponseWithErrorDetailsDto<object, object>) HandleUnexpectedException(Exception ex)
    {
        // In development, include stack trace in error details
        object errorDetails = null;
        if (_environment.IsDevelopment())
        {
            errorDetails = new
            {
                exceptionType = ex.GetType().Name,
                stackTrace = ex.StackTrace,
                innerException = ex.InnerException?.Message
            };
        }

        var errorMessage = _environment.IsDevelopment()
            ? ex.Message
            : "An unexpected error occurred. Please try again later.";

        return (HttpStatusCode.InternalServerError,
            new BaseResponseWithErrorDetailsDto<object, object>(ErrorCodeEnum.InternalError, errorMessage, errorDetails));
    }

    private static (HttpStatusCode, BaseResponseWithErrorDetailsDto<object, object>) HandleExceptionWithStatusAndErrorCodes(
        ExceptionWithStatusAndErrorCodes exception,
        HttpContext context)
    {
        var statusCode = exception.StatusCode;

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