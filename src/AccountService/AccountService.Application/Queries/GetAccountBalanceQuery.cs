using MediatR;

namespace AccountService.Application.Queries;

public record GetAccountBalanceQuery(string AccountId) : IRequest<AccountBalanceResult?>;
public record AccountBalanceResult(string AccountId, decimal Balance);