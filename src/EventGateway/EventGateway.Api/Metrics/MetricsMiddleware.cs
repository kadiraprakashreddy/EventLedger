using EventGateway.Api.Metrics;
using System.Diagnostics;

namespace EventGateway.Api.Metrics;

public class MetricsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly MetricsRegistry _registry;

    public MetricsMiddleware(RequestDelegate next, MetricsRegistry registry)
    {
        _next = next;
        _registry = registry;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();
            var endpoint = $"{context.Request.Method} {context.Request.Path}";
            _registry.Record(endpoint, sw.Elapsed.TotalMilliseconds);
        }
    }
}