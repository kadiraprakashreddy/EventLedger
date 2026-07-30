using AccountService.Domain.Enums;
using MediatR;

namespace AccountService.Application.Commands;

public record ApplyTransactionCommand(
    string AccountId,
    string EventId,
    TransactionType Type,
    decimal Amount,
    string Currency,
    DateTimeOffset EventTimestamp,
    string? TraceId
) : IRequest<ApplyTransactionResult>;

public record ApplyTransactionResult(string AccountId, decimal Balance, bool WasNewlyApplied);