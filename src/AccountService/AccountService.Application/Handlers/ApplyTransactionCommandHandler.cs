using AccountService.Application.Commands;
using AccountService.Application.Interfaces;
using AccountService.Domain.Entities;
using MediatR;

namespace AccountService.Application.Handlers;

public class ApplyTransactionCommandHandler
    : IRequestHandler<ApplyTransactionCommand, ApplyTransactionResult>
{
    private readonly IAccountRepository _repository;

    public ApplyTransactionCommandHandler(IAccountRepository repository)
    {
        _repository = repository;
    }

    public async Task<ApplyTransactionResult> Handle(
        ApplyTransactionCommand request, CancellationToken cancellationToken)
    {
        var account = await _repository.GetByIdAsync(request.AccountId, cancellationToken);
        if (account is null)
        {
            account = Account.Create(request.AccountId);
            await _repository.AddAsync(account, cancellationToken);
        }

        var transaction = Transaction.Create(
            request.EventId, request.AccountId, request.Type,
            request.Amount, request.Currency, request.EventTimestamp, request.TraceId);

        var wasApplied = account.ApplyTransaction(transaction);

        await _repository.SaveChangesAsync(cancellationToken);

        return new ApplyTransactionResult(account.AccountId, account.Balance, wasApplied);
    }
}