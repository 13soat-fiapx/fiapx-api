using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.Json;

namespace FiapX.Api.Middlewares;

public sealed class DomainValidationMiddleware(RequestDelegate next)
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException exception)
        {
            if (context.Response.HasStarted)
                throw;

            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Type = "https://httpstatuses.com/400",
                Title = "One or more validation errors occurred.",
                Detail = "See the errors property for details.",
                Instance = $"{context.Request.Method} {context.Request.Path}",
                Extensions =
                {
                    ["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier,
                    ["requestId"] = context.TraceIdentifier,
                    ["errors"] = exception.Errors
                        .GroupBy(error => error.PropertyName)
                        .ToDictionary(
                            group => group.Key,
                            group => group.Select(error => error.ErrorMessage).ToArray())
                }
            };

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, SerializerOptions));
        }
    }
}
