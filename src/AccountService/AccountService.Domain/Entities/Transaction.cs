using AccountService.Domain.Enums;
using AccountService.Domain.Exceptions;

namespace AccountService.Domain.Entities;

public class Transaction
{
    // surrogate key
    public int Id { get; set; }
    // idempotency guard
    public string EventId { get; set; } = default!;     
    public string AccountId { get; set; } = default!;
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = default!;
    // when the event originally occurred
    public DateTimeOffset EventTimestamp { get; set; }
    // when AccountService processed it
    public DateTimeOffset AppliedAt { get; set; }        
    public string? TraceId { get; set; }

    public static Transaction Create(
        string eventId,
        string accountId,
        TransactionType type,
        decimal amount,
        string currency,
        DateTimeOffset eventTimestamp,
        string? traceId = null)
    {
        if (string.IsNullOrWhiteSpace(eventId))
            throw new InvalidTransactionException("EventId is required.");
        if (string.IsNullOrWhiteSpace(accountId))
            throw new InvalidTransactionException("AccountId is required.");
        if (amount <= 0)
            throw new InvalidTransactionException("Amount must be greater than 0.");
        if (string.IsNullOrWhiteSpace(currency))
            throw new InvalidTransactionException("Currency is required.");

        return new Transaction
        {
            EventId = eventId,
            AccountId = accountId,
            Type = type,
            Amount = amount,
            Currency = currency,
            EventTimestamp = eventTimestamp,
            AppliedAt = DateTimeOffset.UtcNow,
            TraceId = traceId
        };
    }
}