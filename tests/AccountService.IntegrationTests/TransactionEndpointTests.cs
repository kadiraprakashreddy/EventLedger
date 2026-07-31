using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace AccountService.IntegrationTests;

// Each test run gets its own temp SQLite file instead of the fixed "accountservice.db" from
// appsettings.json — otherwise a leftover file from a previous `dotnet test` run makes the
// "first" submission of a fixed eventId look like a duplicate, and this test flips from
// Created to OK depending on what ran before it.
public class TransactionEndpointTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"accountservice-test-{Guid.NewGuid():N}.db");
    private readonly WebApplicationFactory<AccountService.Api.Program> _factory;
    private readonly HttpClient _client;

    public TransactionEndpointTests()
    {
        _factory = new WebApplicationFactory<AccountService.Api.Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:AccountDb"] = $"Data Source={_dbPath}"
                    });
                });
            });
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        SqliteConnection.ClearAllPools(); // release the pooled connection before deleting the file
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
    }

    [Fact]
    public async Task ApplyTransaction_DuplicateEventId_ReturnsOkNotCreated()
    {
        var payload = new { eventId = "evt-http-1", type = "CREDIT", amount = 50m, currency = "USD", eventTimestamp = DateTimeOffset.UtcNow };

        var first = await _client.PostAsJsonAsync("/accounts/acct-http-1/transactions", payload);
        var second = await _client.PostAsJsonAsync("/accounts/acct-http-1/transactions", payload);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
    }

    [Fact]
    public async Task ApplyTransaction_NegativeAmount_ReturnsBadRequest()
    {
        var payload = new { eventId = "evt-http-2", type = "CREDIT", amount = -10m, currency = "USD", eventTimestamp = DateTimeOffset.UtcNow };

        var response = await _client.PostAsJsonAsync("/accounts/acct-http-2/transactions", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}