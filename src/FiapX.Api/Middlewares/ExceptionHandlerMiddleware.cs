using FiapX.Domain.Base.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Diagnostics;
using System.Text.Json;

namespace FiapX.Api.Middlewares;

public sealed class ExceptionHandlerMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlerMiddleware> logger,
    IHostEnvironment environment)
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            if (context.Response.HasStarted)
                throw;

            var statusCode = ToStatusCode(exception);
            var routePattern = (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText;
            var route = routePattern ?? context.Request.Path.ToString();

            if (statusCode == StatusCodes.Status500InternalServerError)
            {
                logger.LogError(
                    exception,
                    "Unhandled exception while processing {HttpMethod} {HttpRoute}",
                    context.Request.Method,
                    route);
            }
            else
            {
                logger.LogWarning(
                    exception,
                    "Handled exception while processing {HttpMethod} {HttpRoute}",
                    context.Request.Method,
                    route);
            }

            await WriteProblemDetailsAsync(context, statusCode, exception);
        }
    }

    private async Task WriteProblemDetailsAsync(
        HttpContext context,
        int statusCode,
        Exception exception)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Type = $"https://httpstatuses.com/{statusCode}",
            Title = ToTitle(statusCode),
            Detail = ToDetail(statusCode, exception),
            Instance = $"{context.Request.Method} {context.Request.Path}",
            Extensions =
            {
                ["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier,
                ["requestId"] = context.TraceIdentifier
            }
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, SerializerOptions));
    }

    private static int ToStatusCode(Exception exception)
    {
        return exception switch
        {
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            EntityNotFoundException => StatusCodes.Status404NotFound,
            ForbiddenException => StatusCodes.Status403Forbidden,
            ConflictException => StatusCodes.Status409Conflict,
            BusinessException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };
    }

    private string ToDetail(int statusCode, Exception exception)
    {
        if (statusCode != StatusCodes.Status500InternalServerError || environment.IsDevelopment())
            return exception.Message;

        return "An unexpected error occurred while processing the request.";
    }

    private static string ToTitle(int statusCode)
    {
        var title = ReasonPhrases.GetReasonPhrase(statusCode);
        return string.IsNullOrWhiteSpace(title) ? "Error" : title;
    }
}
