using EventGateway.Domain.Enums;
using MediatR;

namespace EventGateway.Application.Commands;

public record SubmitEventCommand(
    string EventId,
    string AccountId,
    TransactionType Type,
    decimal Amount,
    string Currency,
    DateTimeOffset EventTimestamp,
    string? MetadataJson,
    string TraceId
) : IRequest<SubmitEventResult>;

public enum SubmitEventOutcome
{
    Created,                // new event, applied downstream -> 201
    Duplicate,              // already existed -> 200
    DownstreamUnavailable,  // stored, but AccountService unreachable -> 503
    DownstreamRejected      // stored, AccountService rejected it -> 502
}

public record SubmitEventResult(
    string EventId,
    SubmitEventOutcome Outcome,
    EventStatus Status,
    decimal? AccountBalance,
    string? ErrorMessage);