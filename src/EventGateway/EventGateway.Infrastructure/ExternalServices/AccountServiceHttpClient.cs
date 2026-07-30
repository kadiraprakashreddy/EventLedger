using System.Net.Http.Json;
using EventGateway.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;

namespace EventGateway.Infrastructure.ExternalServices;

public class AccountServiceHttpClient : IAccountServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AccountServiceHttpClient> _logger;

    public AccountServiceHttpClient(HttpClient httpClient, ILogger<AccountServiceHttpClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<AccountServiceCallResult> ApplyTransactionAsync(
        string accountId, ApplyTransactionRequest request, string traceId, CancellationToken ct)
    {
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"/accounts/{accountId}/transactions")
            {
                Content = JsonContent.Create(request)
            };
            httpRequest.Headers.Add("X-Trace-Id", traceId); // trace propagation

            var response = await _httpClient.SendAsync(httpRequest, ct);

            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadFromJsonAsync<AccountServiceTransactionResponse>(cancellationToken: ct);
                return new AccountServiceCallResult(AccountServiceCallStatus.Success, body?.Balance, null);
            }

            _logger.LogWarning("AccountService rejected transaction. Status={Status} TraceId={TraceId}", response.StatusCode, traceId);
            return new AccountServiceCallResult(AccountServiceCallStatus.Rejected, null, $"AccountService returned {response.StatusCode}");
        }
        catch (BrokenCircuitException)
        {
            _logger.LogWarning("Circuit open — short-circuiting call to AccountService. TraceId={TraceId}", traceId);
            return new AccountServiceCallResult(AccountServiceCallStatus.Unavailable, null, "AccountService is currently unavailable (circuit open).");
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("AccountService call timed out. TraceId={TraceId}", traceId);
            return new AccountServiceCallResult(AccountServiceCallStatus.Unavailable, null, "AccountService request timed out.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "AccountService unreachable. TraceId={TraceId}", traceId);
            return new AccountServiceCallResult(AccountServiceCallStatus.Unavailable, null, "AccountService is unreachable.");
        }
    }
}

internal record AccountServiceTransactionResponse(string AccountId, decimal Balance, bool WasNewlyApplied);