using EventGateway.Application.Interfaces;
using MediatR;

namespace EventGateway.Application.Queries;

public record GetAccountBalanceQuery(string AccountId, string TraceId) : IRequest<AccountBalanceResult>;

public record AccountBalanceResult(
    string AccountId, AccountServiceCallStatus Status, decimal? Balance, string? ErrorMessage);
