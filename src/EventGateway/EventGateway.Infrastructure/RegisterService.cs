using EventGateway.Application.Interfaces;
using EventGateway.Infrastructure.Data;
using EventGateway.Infrastructure.ExternalServices;
using EventGateway.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;

namespace EventGateway.Infrastructure;

public static class RegisterService
{
    public static IServiceCollection AddEventGateWayInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<EventDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("EventDb") ?? "Data Source=eventgateway.db"));

        services.AddScoped<IEventRepository, EventRepository>();
        services.AddHttpClient<IAccountServiceClient, AccountServiceHttpClient>(client =>
        {
            client.BaseAddress = new Uri(configuration["AccountService:BaseUrl"] ?? "http://localhost:5005");
            client.Timeout = TimeSpan.FromSeconds(5);
        })
       .AddPolicyHandler((serviceProvider, request) => GetCircuitBreakerPolicy(serviceProvider));


        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("CircuitBreaker");

        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 3,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (outcome, breakDelay) =>
                    logger.LogWarning("Circuit OPENED for {Seconds}s. Reason: {Reason}",
                        breakDelay.TotalSeconds, outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString()),
                onReset: () =>
                    logger.LogInformation("Circuit CLOSED — AccountService calls resumed."),
                onHalfOpen: () =>
                    logger.LogInformation("Circuit HALF-OPEN — next call will test AccountService."));
    }
}