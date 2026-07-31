using System.Text.Json;
using EventGateway.Api.Logging;
using EventGateway.Api.Metrics;
using EventGateway.Api.Middleware;
using EventGateway.Application.Handlers;
using EventGateway.Infrastructure;
using EventGateway.Infrastructure.Data;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;
using Serilog.Formatting.Json;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "EventGateway")
    .MinimumLevel.Information()
    .WriteTo.Console(new LogFormatter())
    .WriteTo.File(new LogFormatter(), "logs/eventgateway-.json", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddEventGateWayInfrastructure(builder.Configuration);
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(SubmitEventCommandHandler).Assembly));

builder.Services.AddHealthChecks()
    .AddDbContextCheck<EventDbContext>();
builder.Services.AddSingleton<MetricsRegistry>();


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EventDbContext>();
    db.Database.EnsureCreated();
}
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RequestTracingMiddleware>();
app.UseMiddleware<MetricsMiddleware>();
app.UseSerilogRequestLogging();
app.MapControllers();
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            service = "EventGateway",
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description
            })
        }));
    }
});
app.MapGet("/metrics", (MetricsRegistry registry) => Results.Ok(registry.Snapshot()));

try
{
    app.Run();
}
finally
{
    Log.CloseAndFlush();
}

namespace EventGateway.Api
{
    public partial class Program { } // for integration tests
}