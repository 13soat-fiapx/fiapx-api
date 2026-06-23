using System.Diagnostics;

namespace FiapX.Api.Middlewares;

public sealed class RequestLoggingMiddleware(
    RequestDelegate next,
    ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await next(context);
        }
        finally
        {
            stopwatch.Stop();

            var routePattern = (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText;
            var route = routePattern ?? context.Request.Path.ToString();
            var statusCode = context.Response.StatusCode;
            var level =
                statusCode >= 500 ? LogLevel.Error :
                statusCode >= 400 ? LogLevel.Warning :
                LogLevel.Information;

            logger.Log(
                level,
                "HTTP request completed | {HttpMethod} {HttpRoute} {StatusCode} {ElapsedMilliseconds}ms",
                context.Request.Method,
                route,
                statusCode,
                stopwatch.ElapsedMilliseconds);
        }
    }
}
