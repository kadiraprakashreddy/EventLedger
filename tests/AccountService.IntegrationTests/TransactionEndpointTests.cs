using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace AccountService.IntegrationTests;

public class TransactionEndpointTests : IClassFixture<WebApplicationFactory<AccountService.Api.Program>>
{
    private readonly HttpClient _client;

    public TransactionEndpointTests(WebApplicationFactory<AccountService.Api.Program> factory)
    {
        _client = factory.CreateClient();
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