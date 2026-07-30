namespace EventGateway.Api.Middleware;
    public class RequestTracingMiddleware
    {
        private readonly RequestDelegate _next;

        public RequestTracingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var traceId = context.Request.Headers.TryGetValue("X-Trace-Id", out var incoming) && !string.IsNullOrWhiteSpace(incoming)
                ? incoming.ToString()
                : Guid.NewGuid().ToString("N");

            context.Items["TraceId"] = traceId;
            context.Response.Headers["X-Trace-Id"] = traceId;

            await _next(context);
        }
    }
