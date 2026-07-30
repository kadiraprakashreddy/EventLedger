using EventGateway.Application.Interfaces;
using EventGateway.Infrastructure.Data;
using EventGateway.Infrastructure.ExternalServices;
using EventGateway.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
       .AddPolicyHandler(GetCircuitBreakerPolicy());

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError() // 5xx, 408, and connection failures
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 3,
                durationOfBreak: TimeSpan.FromSeconds(30));
    }
}