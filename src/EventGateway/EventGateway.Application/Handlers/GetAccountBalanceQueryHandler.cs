using EventGateway.Application.Interfaces;
using EventGateway.Application.Queries;
using MediatR;

namespace EventGateway.Application.Handlers;

public class GetAccountBalanceQueryHandler : IRequestHandler<GetAccountBalanceQuery, AccountBalanceResult>
{
    private readonly IAccountServiceClient _accountServiceClient;

    public GetAccountBalanceQueryHandler(IAccountServiceClient accountServiceClient)
        => _accountServiceClient = accountServiceClient;

    public async Task<AccountBalanceResult> Handle(GetAccountBalanceQuery request, CancellationToken ct)
    {
        var result = await _accountServiceClient.GetBalanceAsync(request.AccountId, request.TraceId, ct);
        return new AccountBalanceResult(request.AccountId, result.Status, result.Balance, result.ErrorMessage);
    }
}
