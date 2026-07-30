using EventGateway.Api.Logging;
using EventGateway.Api.Metrics;
using EventGateway.Api.Middleware;
using EventGateway.Application.Handlers;
using EventGateway.Infrastructure;
using EventGateway.Infrastructure.Data;
using Serilog;
using Serilog.Formatting.Json;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "EventGateway")
    .MinimumLevel.Information()
    .WriteTo.Console(new LogFormatter())
    .WriteTo.File(new LogFormatter(), "logs/accountservice-.json", rollingInterval: RollingInterval.Day)
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
app.MapHealthChecks("/health");
app.MapGet("/metrics", (MetricsRegistry registry) => Results.Ok(registry.Snapshot()));

try
{
    app.Run();
}
finally
{
    Log.CloseAndFlush();
}