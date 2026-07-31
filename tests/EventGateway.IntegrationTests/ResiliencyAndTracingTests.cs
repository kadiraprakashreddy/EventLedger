using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AccountService.Api;
using EventGateway.Api;
using EventGateway.Application.Interfaces;
using EventGateway.Infrastructure.ExternalServices;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace EventGateway.IntegrationTests;

/// <summary>
/// Requirement #8 (Resiliency behavior): simulates AccountService being unreachable
/// and verifies the Gateway degrades gracefully instead of hanging or returning a 500.
/// </summary>
public class ResiliencyTests : IDisposable
{
    private readonly WebApplicationFactory<EventGateway.Api.Program> _gatewayFactory;
    private readonly HttpClient _gatewayClient;

    public ResiliencyTests()
    {
        // Point the real AccountServiceHttpClient + Polly circuit breaker (as configured in
        // RegisterService.cs) at a loopback port nothing is listening on, so every call fails
        // with a connection error — the same failure mode as AccountService actually being down.
        _gatewayFactory = new WebApplicationFactory<EventGateway.Api.Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["AccountService:BaseUrl"] = "http://127.0.0.1:1"
                    });
                });
            });

        _gatewayClient = _gatewayFactory.CreateClient();
    }

    [Fact]
    public async Task SubmitEvent_AccountServiceUnreachable_Returns503NotHangOrError500()
    {
        var payload = new
        {
            eventId = $"evt-resiliency-{Guid.NewGuid():N}",
            accountId = "acct-resiliency-1",
            type = "CREDIT",
            amount = 42.00m,
            currency = "USD",
            eventTimestamp = DateTimeOffset.UtcNow
        };

        var response = await _gatewayClient.PostAsJsonAsync("/events", payload);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("DownstreamUnavailable", body.RootElement.GetProperty("outcome").GetString());
    }

    [Fact]
    public async Task SubmitEvent_RepeatedFailures_KeepsReturning503WithoutHanging()
    {
        // Exceeds the breaker's handledEventsAllowedBeforeBreaking (3): first few calls fail the
        // real HTTP attempt, later ones are short-circuited by the open circuit — either way the
        // client-visible contract must hold: fast, graceful 503, never a hang or a 500.
        for (var i = 0; i < 5; i++)
        {
            var payload = new
            {
                eventId = $"evt-resiliency-repeat-{i}-{Guid.NewGuid():N}",
                accountId = "acct-resiliency-2",
                type = "CREDIT",
                amount = 10.00m,
                currency = "USD",
                eventTimestamp = DateTimeOffset.UtcNow
            };

            var response = await _gatewayClient.PostAsJsonAsync("/events", payload);

            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        }
    }

    [Fact]
    public async Task GetEventById_StillWorks_WhenAccountServiceUnreachable()
    {
        // Gateway reads are served from its own local DB and must keep working
        // even when the downstream AccountService is completely unreachable.
        var eventId = $"evt-resiliency-read-{Guid.NewGuid():N}";
        var payload = new
        {
            eventId,
            accountId = "acct-resiliency-3",
            type = "CREDIT",
            amount = 5.00m,
            currency = "USD",
            eventTimestamp = DateTimeOffset.UtcNow
        };

        await _gatewayClient.PostAsJsonAsync("/events", payload); // stored locally even though downstream fails

        var response = await _gatewayClient.GetAsync($"/events/{eventId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    public void Dispose()
    {
        _gatewayClient.Dispose();
        _gatewayFactory.Dispose();
    }
}

/// <summary>
/// Requirement #8 (Trace propagation): verifies the trace ID supplied by the caller flows
/// unchanged from the Gateway's inbound request to the actual HTTP call made against AccountService.
/// </summary>
public class TracePropagationTests : IDisposable
{
    private readonly WebApplicationFactory<AccountService.Api.Program> _accountServiceFactory;
    private readonly WebApplicationFactory<EventGateway.Api.Program> _gatewayFactory;
    private readonly HttpClient _gatewayClient;
    private readonly CapturingHandler _capturingHandler;

    public TracePropagationTests()
    {
        _accountServiceFactory = new WebApplicationFactory<AccountService.Api.Program>();
        _capturingHandler = new CapturingHandler();

        // CreateDefaultClient lets us splice a spy handler in front of the real TestServer
        // handler, so we can inspect the exact headers AccountService receives.
        var accountServiceClient = _accountServiceFactory.CreateDefaultClient(_capturingHandler);

        _gatewayFactory = new WebApplicationFactory<EventGateway.Api.Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll(typeof(IAccountServiceClient));
                    services.AddSingleton<IAccountServiceClient>(sp =>
                        new AccountServiceHttpClient(accountServiceClient, sp.GetRequiredService<ILogger<AccountServiceHttpClient>>()));
                });
            });

        _gatewayClient = _gatewayFactory.CreateClient();
    }

    [Fact]
    public async Task SubmitEvent_PropagatesCallerTraceId_ToAccountServiceRequest()
    {
        const string callerTraceId = "trace-e2e-12345";

        using var request = new HttpRequestMessage(HttpMethod.Post, "/events")
        {
            Content = JsonContent.Create(new
            {
                eventId = $"evt-trace-{Guid.NewGuid():N}",
                accountId = "acct-trace-1",
                type = "CREDIT",
                amount = 15.00m,
                currency = "USD",
                eventTimestamp = DateTimeOffset.UtcNow
            })
        };
        request.Headers.Add("X-Trace-Id", callerTraceId);

        var response = await _gatewayClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        // The Gateway echoes back the same trace ID it used for the whole request...
        Assert.Equal(callerTraceId, response.Headers.GetValues("X-Trace-Id").Single());

        // ...and that is the exact trace ID it forwarded on the outbound call to AccountService.
        Assert.Equal(callerTraceId, _capturingHandler.LastRequestTraceId);
    }

    public void Dispose()
    {
        _gatewayClient.Dispose();
        _gatewayFactory.Dispose();
        _accountServiceFactory.Dispose();
    }

    private class CapturingHandler : DelegatingHandler
    {
        public string? LastRequestTraceId { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestTraceId = request.Headers.TryGetValues("X-Trace-Id", out var values)
                ? values.FirstOrDefault()
                : null;
            return base.SendAsync(request, cancellationToken);
        }
    }
}
