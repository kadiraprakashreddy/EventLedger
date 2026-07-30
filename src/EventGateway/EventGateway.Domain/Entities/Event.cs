using EventGateway.Domain.Enums;
using EventGateway.Domain.Exceptions;

namespace EventGateway.Domain.Entities;

public class Event
{
    public string EventId { get; set; } = default!;      // PK — natural idempotency key
    public string AccountId { get; set; } = default!;
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = default!;
    public DateTimeOffset EventTimestamp { get; set; }   // when the event originally occurred
    public string? MetadataJson { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }
    public EventStatus Status { get; set; }
    public string? TraceId { get; set; }

    public static Event Create(
        string eventId,
        string accountId,
        TransactionType type,
        decimal amount,
        string currency,
        DateTimeOffset eventTimestamp,
        string? metadataJson,
        string? traceId)
    {
        if (string.IsNullOrWhiteSpace(eventId))
            throw new InvalidEventException("EventId is required.");
        if (string.IsNullOrWhiteSpace(accountId))
            throw new InvalidEventException("AccountId is required.");
        if (amount <= 0)
            throw new InvalidEventException("Amount must be greater than 0.");
        if (string.IsNullOrWhiteSpace(currency))
            throw new InvalidEventException("Currency is required.");

        return new Event
        {
            EventId = eventId,
            AccountId = accountId,
            Type = type,
            Amount = amount,
            Currency = currency,
            EventTimestamp = eventTimestamp,
            MetadataJson = metadataJson,
            ReceivedAt = DateTimeOffset.UtcNow,
            Status = EventStatus.Received,
            TraceId = traceId
        };
    }

    public void MarkApplied() => Status = EventStatus.Applied;
    public void MarkFailed() => Status = EventStatus.Failed;
}