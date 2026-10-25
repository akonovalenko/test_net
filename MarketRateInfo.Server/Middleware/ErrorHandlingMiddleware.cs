using MarketRateInfo.Server.BusinessLogic.Exceptions;
using MarketRateInfo.Server.BusinessLogic.Models.Responses;
using System.Text.Json;

namespace MarketRateInfo.Server.Middleware;

/// <summary>
/// Middleware for handling unhandled exceptions in the application.
/// </summary>
/// <param name="next">The next middleware in the pipeline.</param>
/// <param name="environment">The host environment.</param>
/// <param name="logger">The logger.</param>
public sealed class ErrorHandlingMiddleware(
    RequestDelegate next,
    IHostEnvironment environment,
    ILogger<ErrorHandlingMiddleware> logger)
{
    /// <summary>
    /// Invokes the middleware to handle exceptions during the request processing.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    /// <summary>
    /// Handles the exception by logging it and returning an appropriate error response.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="exception">The exception to handle.</param>
    /// <returns>The task representing the asynchronous operation.</returns>
    private async Task HandleExceptionAsync(
        HttpContext context,
        Exception exception)
    {
        var traceId = context.TraceIdentifier;

        logger.LogError(
            exception,
            "Unhandled exception. TraceId: {TraceId}, Method: {Method}, Path: {Path}",
            traceId,
            context.Request.Method,
            context.Request.Path);

        if (context.Response.HasStarted)
        {
            logger.LogWarning(
                "Cannot write error response because the response has already started. TraceId: {TraceId}",
                traceId);

            throw exception;
        }

        var statusCode = exception is BusinessValidationException
            ? StatusCodes.Status400BadRequest
            : StatusCodes.Status500InternalServerError;

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = new ErrorResponse
        {
            StatusCode = statusCode,
            Message = exception is BusinessValidationException || environment.IsDevelopment()
                ? exception.Message
                : "An unexpected error occurred.",
            TraceId = traceId,
            Detail = environment.IsDevelopment() && exception is not BusinessValidationException
                ? exception.ToString()
                : null
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
