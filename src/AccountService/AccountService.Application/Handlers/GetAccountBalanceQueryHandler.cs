using AccountService.Application.Interfaces;
using AccountService.Application.Queries;
using MediatR;

namespace AccountService.Application.Handlers;

public class GetAccountBalanceQueryHandler
    : IRequestHandler<GetAccountBalanceQuery, AccountBalanceResult?>
{
    private readonly IAccountRepository _repository;

    public GetAccountBalanceQueryHandler(IAccountRepository repository)
    {
        _repository = repository;
    }

    public async Task<AccountBalanceResult?> Handle(
        GetAccountBalanceQuery request, CancellationToken cancellationToken)
    {
        var account = await _repository.GetByIdAsync(request.AccountId, cancellationToken);
        return account is null ? null : new AccountBalanceResult(account.AccountId, account.Balance);
    }
}