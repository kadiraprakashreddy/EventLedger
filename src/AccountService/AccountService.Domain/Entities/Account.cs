using AccountService.Domain.Enums;
using AccountService.Domain.Exceptions;

namespace AccountService.Domain.Entities;

public class Account
{
    public string AccountId { get; set; } = default!;
    // derived cache — only ApplyTransaction may change it
    public decimal Balance { get; set; }                 
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public List<Transaction> Transactions { get; set; } = new();

    public static Account Create(string accountId)
    {
        if (string.IsNullOrWhiteSpace(accountId))
            throw new InvalidTransactionException("AccountId is required.");

        return new Account
        {
            AccountId = accountId,
            Balance = 0m,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Applies a transaction to the account. Idempotent: if a transaction
    /// with the same EventId was already applied, this is a no-op.
    /// Returns true if the transaction was newly applied, false if it was a duplicate.
    /// </summary>
    public bool ApplyTransaction(Transaction transaction)
    {
        if (Transactions.Any(t => t.EventId == transaction.EventId))
            return false; // already applied — idempotent no-op

        var delta = transaction.Type == TransactionType.CREDIT
            ? transaction.Amount
            : -transaction.Amount;

        Balance += delta;
        UpdatedAt = DateTimeOffset.UtcNow;
        Transactions.Add(transaction);
        return true;
    }
}