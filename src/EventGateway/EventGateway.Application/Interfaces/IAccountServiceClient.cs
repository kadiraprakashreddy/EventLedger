using EventGateway.Domain.Enums;

namespace EventGateway.Application.Interfaces;

public enum AccountServiceCallStatus
{
    Success,
    Unavailable,   // circuit open, timeout, connection refused
    Rejected       // AccountService reachable and returned an error
}

public record ApplyTransactionRequest(
    string EventId, TransactionType Type, decimal Amount, string Currency, DateTimeOffset EventTimestamp);

public record AccountServiceCallResult(
    AccountServiceCallStatus Status, decimal? Balance, string? ErrorMessage);

public interface IAccountServiceClient
{
    Task<AccountServiceCallResult> ApplyTransactionAsync(
        string accountId, ApplyTransactionRequest request, string traceId, CancellationToken ct);

    Task<AccountServiceCallResult> GetBalanceAsync(
        string accountId, string traceId, CancellationToken ct);
}