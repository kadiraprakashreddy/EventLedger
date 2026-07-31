using System.Net;
using System.Net.Http.Json;
using AccountService.Api;
using EventGateway.Api;
using EventGateway.Application.Interfaces;
using EventGateway.Infrastructure.ExternalServices;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace EventGateway.IntegrationTests;

// Each test run gets its own temp SQLite files for both services — otherwise leftover
// "accountservice.db"/"eventgateway.db" files from a previous `dotnet test` run make this
// test's fixed eventId look like a duplicate on the next run.
public class GatewayToAccountServiceFlowTests : IDisposable
{
    private readonly string _accountDbPath = Path.Combine(Path.GetTempPath(), $"accountservice-test-{Guid.NewGuid():N}.db");
    private readonly string _eventDbPath = Path.Combine(Path.GetTempPath(), $"eventgateway-test-{Guid.NewGuid():N}.db");
    private readonly WebApplicationFactory<AccountService.Api.Program> _accountServiceFactory;
    private readonly WebApplicationFactory<EventGateway.Api.Program> _gatewayFactory;
    private readonly HttpClient _gatewayClient;
    private readonly HttpClient _accountServiceClient;

    public GatewayToAccountServiceFlowTests()
    {
        _accountServiceFactory = new WebApplicationFactory<AccountService.Api.Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:AccountDb"] = $"Data Source={_accountDbPath}"
                    });
                });
            });
        _accountServiceClient = _accountServiceFactory.CreateClient();

        _gatewayFactory = new WebApplicationFactory<EventGateway.Api.Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:EventDb"] = $"Data Source={_eventDbPath}"
                    });
                });
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

    [Fact]
    public async Task GetBalance_ProxiesToAccountService_ReturnsCurrentBalance()
    {
        var payload = new
        {
            eventId = "evt-int-balance-1",
            accountId = "acct-int-balance-1",
            type = "CREDIT",
            amount = 75.00m,
            currency = "USD",
            eventTimestamp = DateTimeOffset.UtcNow
        };

        var submit = await _gatewayClient.PostAsJsonAsync("/events", payload);
        Assert.Equal(HttpStatusCode.Created, submit.StatusCode);

        var response = await _gatewayClient.GetAsync("/accounts/acct-int-balance-1/balance");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var balance = await response.Content.ReadFromJsonAsync<BalanceDto>();
        Assert.NotNull(balance);
        Assert.Equal(75.00m, balance!.Balance);
    }

    public void Dispose()
    {
        _gatewayClient.Dispose();
        _accountServiceClient.Dispose();
        _accountServiceFactory.Dispose();
        _gatewayFactory.Dispose();
        SqliteConnection.ClearAllPools(); // release pooled connections before deleting the files
        if (File.Exists(_accountDbPath)) File.Delete(_accountDbPath);
        if (File.Exists(_eventDbPath)) File.Delete(_eventDbPath);
    }
}