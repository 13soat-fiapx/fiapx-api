using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FiapX.Api.Middlewares;

public sealed class RequestValidationFilter(
    ILogger<RequestValidationFilter> logger,
    IServiceProvider serviceProvider) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var failures = new List<ValidationFailure>();

        foreach (var argument in context.ActionArguments.Values)
        {
            var validationResult = await ValidateAsync(argument, context.HttpContext.RequestAborted);
            if (validationResult is not { IsValid: false })
                continue;

            failures.AddRange(validationResult.Errors);
        }

        if (failures.Count == 0)
        {
            await next();
            return;
        }

        logger.LogInformation("Request validation failed for action: {Action}", context.ActionDescriptor.DisplayName);

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred.",
            Type = "https://httpstatuses.com/400",
            Detail = "See the errors property for details."
        };
        problemDetails.Extensions["errors"] = failures
            .GroupBy(error => ToCamelCasePath(error.PropertyName))
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.ErrorMessage).ToArray());

        context.Result = new BadRequestObjectResult(problemDetails)
        {
            ContentTypes = { "application/problem+json" }
        };
    }

    private async Task<ValidationResult?> ValidateAsync(object? argument, CancellationToken cancellationToken)
    {
        if (argument is null)
            return null;

        var argumentType = argument.GetType();
        if (argumentType.IsPrimitive || argumentType == typeof(string) || argumentType == typeof(CancellationToken))
            return null;

        var validatorType = typeof(IValidator<>).MakeGenericType(argumentType);
        if (serviceProvider.GetService(validatorType) is not IValidator validator)
            return null;

        var context = new ValidationContext<object>(argument);
        return await validator.ValidateAsync(context, cancellationToken);
    }

    private static string ToCamelCasePath(string propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
            return propertyName;

        var segments = propertyName.Split('.', StringSplitOptions.RemoveEmptyEntries);
        return string.Join('.', segments.Select(ToCamelCase));
    }

    private static string ToCamelCase(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? value
            : string.Concat(char.ToLowerInvariant(value[0]), value[1..]);
    }
}
