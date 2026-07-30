using AccountService.Application.Interfaces;
using AccountService.Application.Queries;
using MediatR;

namespace AccountService.Application.Handlers;

public class GetAccountQueryHandler : IRequestHandler<GetAccountQuery, AccountDetailsResult?>
{
    private readonly IAccountRepository _repository;

    public GetAccountQueryHandler(IAccountRepository repository)
    {
        _repository = repository;
    }
   
    public async Task<AccountDetailsResult?> Handle(GetAccountQuery request, CancellationToken cancellationToken)
    {
        var account = await _repository.GetByIdAsync(request.AccountId, cancellationToken);
        if (account is null) return null;

        var recent = account.Transactions
            .OrderByDescending(t => t.EventTimestamp)
            .Take(10)
            .Select(t => new TransactionDto(
                t.EventId, t.Type.ToString(), t.Amount, t.Currency, t.EventTimestamp, t.AppliedAt))
            .ToList();

        return new AccountDetailsResult(account.AccountId, account.Balance, account.CreatedAt, account.UpdatedAt, recent);
    }
}