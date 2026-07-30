using Serilog.Context;

namespace EventGateway.Api.Middleware;

public class TraceIdMiddleware
{
    private readonly RequestDelegate _next;

    public TraceIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var traceId = context.Request.Headers.TryGetValue("X-Trace-Id", out var incoming) && !string.IsNullOrWhiteSpace(incoming)
            ? incoming.ToString()
            : Guid.NewGuid().ToString("N");

        context.Items["TraceId"] = traceId;
        context.Response.Headers["X-Trace-Id"] = traceId;

        using (LogContext.PushProperty("TraceId", traceId))
        {
            await _next(context);
        }
    }
}