using AccountService.Domain.Entities;

namespace AccountService.Application.Interfaces;

public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(string accountId, CancellationToken ct);
    Task AddAsync(Account account, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}