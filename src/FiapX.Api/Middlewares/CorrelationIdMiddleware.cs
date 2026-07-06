using System.Diagnostics;

namespace FiapX.Api.Middlewares;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string Header = "X-Correlation-ID";
    public const string ItemsKey = "CorrelationId";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[Header].FirstOrDefault()
            ?? Guid.NewGuid().ToString("N");

        context.Items[ItemsKey] = correlationId;

        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey(Header))
                context.Response.Headers[Header] = correlationId;

            if (Activity.Current?.Id is { } traceparent &&
                !context.Response.Headers.ContainsKey("traceparent"))
                context.Response.Headers["traceparent"] = traceparent;

            return Task.CompletedTask;
        });

        await next(context);
    }
}
