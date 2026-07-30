using MediatR;

namespace AccountService.Application.Queries;

public record GetAccountQuery(string AccountId) : IRequest<AccountDetailsResult?>;

public record AccountDetailsResult(
    string AccountId,
    decimal Balance,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    List<TransactionDto> RecentTransactions);

public record TransactionDto(
    string EventId,
    string Type,
    decimal Amount,
    string Currency,
    DateTimeOffset EventTimestamp,
    DateTimeOffset AppliedAt);