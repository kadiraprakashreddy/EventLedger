using System.Net;
using System.Net.Http.Json;
using AccountService.Api;
using EventGateway.Api;
using EventGateway.Application.Interfaces;
using EventGateway.Infrastructure.ExternalServices;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace EventGateway.IntegrationTests;

public class GatewayToAccountServiceFlowTests : IDisposable
{
    private readonly WebApplicationFactory<AccountService.Api.Program> _accountServiceFactory;
    private readonly WebApplicationFactory<EventGateway.Api.Program> _gatewayFactory;
    private readonly HttpClient _gatewayClient;
    private readonly HttpClient _accountServiceClient;

    public GatewayToAccountServiceFlowTests()
    {
        _accountServiceFactory = new WebApplicationFactory<AccountService.Api.Program>();
        _accountServiceClient = _accountServiceFactory.CreateClient();

        _gatewayFactory = new WebApplicationFactory<EventGateway.Api.Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll(typeof(IAccountServiceClient));
                    services.AddSingleton<IAccountServiceClient>(sp =>
                        new AccountServiceHttpClient(_accountServiceClient, sp.GetRequiredService<ILogger<AccountServiceHttpClient>>()));
                });
            });

        _gatewayClient = _gatewayFactory.CreateClient();
    }

    [Fact]
    public async Task SubmitEvent_FlowsThroughToAccountService_AndUpdatesBalance()
    {
        var payload = new
        {
            eventId = "evt-int-1",
            accountId = "acct-int-1",
            type = "CREDIT",
            amount = 250.00m,
            currency = "USD",
            eventTimestamp = DateTimeOffset.UtcNow
        };

        var response = await _gatewayClient.PostAsJsonAsync("/events", payload);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var balance = await _accountServiceClient.GetFromJsonAsync<BalanceDto>("/accounts/acct-int-1/balance");
        Assert.NotNull(balance);
        Assert.Equal(250.00m, balance!.Balance);
    }

    private record BalanceDto(string AccountId, decimal Balance);

    public void Dispose()
    {
        _gatewayClient.Dispose();
        _accountServiceClient.Dispose();
        _accountServiceFactory.Dispose();
        _gatewayFactory.Dispose();
    }
}