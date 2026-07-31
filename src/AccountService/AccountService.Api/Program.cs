using System.Text.Json;
using AccountService.Api.Logging;
using AccountService.Api.Metrics;
using AccountService.Api.Middleware;
using AccountService.Application.Handlers;
using AccountService.Infrastructure;
using  AccountService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;
using Serilog.Formatting.Json;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "AccountService")
    .MinimumLevel.Information()
    .WriteTo.Console(new LogFormatter())
    .WriteTo.File(new LogFormatter(), "logs/accountservice-.json", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
// Infrastructure: DbContext + repositories
builder.Services.AddAccountServiceInfrastructure(builder.Configuration);

// MediatR: register command/query handlers from the Application assembly
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(ApplyTransactionCommandHandler).Assembly));

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AccountDbContext>();
builder.Services.AddSingleton<MetricsRegistry>();


var app = builder.Build();

// Create or update the SQLite schema on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AccountDbContext>(); 

     db.Database.EnsureCreated();

    // for migrations:
    //db.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<TraceIdMiddleware>();
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
            service = "AccountService",
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
namespace AccountService.Api
{
    public partial class Program { } // for integration tests
}