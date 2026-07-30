using AccountService.Api.Middleware;
using AccountService.Application.Handlers;
using AccountService.Infrastructure;
using  AccountService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

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
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();