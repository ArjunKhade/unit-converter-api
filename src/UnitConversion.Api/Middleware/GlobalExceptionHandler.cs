using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using UnitConversion.Domain.Exceptions;

namespace UnitConversion.Api.Middleware;

/// <summary>
/// Global exception handler that catches unhandled exceptions and formats them as RFC 7807 ProblemDetails.
/// </summary>
public sealed partial class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment environment)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        LogUnhandledException(_logger, exception, exception.Message);

        var (statusCode, title, type) = exception switch
        {
            CategoryNotFoundException => (
                StatusCodes.Status404NotFound,
                "Category Not Found",
                "https://tools.ietf.org/html/rfc7231#section-6.5.4"
            ),
            UnitNotFoundException => (
                StatusCodes.Status400BadRequest,
                "Unit Not Found",
                "https://tools.ietf.org/html/rfc7231#section-6.5.1"
            ),
            UnitConversionValidationException => (
                StatusCodes.Status400BadRequest,
                "Validation Error",
                "https://tools.ietf.org/html/rfc7231#section-6.5.1"
            ),
            BadHttpRequestException => (
                StatusCodes.Status400BadRequest,
                "Bad Request",
                "https://tools.ietf.org/html/rfc7231#section-6.5.1"
            ),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                "https://tools.ietf.org/html/rfc7231#section-6.6.1"
            )
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Type = type,
            Detail = statusCode == StatusCodes.Status500InternalServerError && !_environment.IsDevelopment()
                ? "An internal server error occurred. Please try again later."
                : exception.Message,
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        if (exception is CategoryNotFoundException catEx)
        {
            problemDetails.Extensions["category"] = catEx.Category;
        }
        else if (exception is UnitNotFoundException unitEx)
        {
            problemDetails.Extensions["unit"] = unitEx.Unit;
            problemDetails.Extensions["category"] = unitEx.Category;
        }
        else if (exception is UnitConversionValidationException valEx)
        {
            problemDetails.Extensions["errors"] = valEx.Errors;
        }

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken).ConfigureAwait(false);
        return true;
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Unhandled exception caught by global handler: {Message}")]
    private static partial void LogUnhandledException(ILogger logger, Exception ex, string message);
}
