using AccountService.Domain.Entities;
using AccountService.Domain.Enums;
using AccountService.Domain.Exceptions;
using Xunit;

namespace AccountService.UnitTests;

public class AccountTests
{
    [Fact]
    public void ApplyTransaction_Credit_IncreasesBalance()
    {
        var account = Account.Create("acct-1");
        var tx = Transaction.Create("evt-1", "acct-1", TransactionType.CREDIT, 100m, "USD", DateTimeOffset.UtcNow);

        var applied = account.ApplyTransaction(tx);

        Assert.True(applied);
        Assert.Equal(100m, account.Balance);
    }

    [Fact]
    public void ApplyTransaction_Debit_DecreasesBalance()
    {
        var account = Account.Create("acct-1");
        account.ApplyTransaction(Transaction.Create("evt-1", "acct-1", TransactionType.CREDIT, 100m, "USD", DateTimeOffset.UtcNow));
        account.ApplyTransaction(Transaction.Create("evt-2", "acct-1", TransactionType.DEBIT, 30m, "USD", DateTimeOffset.UtcNow));

        Assert.Equal(70m, account.Balance);
    }

    [Fact]
    public void ApplyTransaction_DuplicateEventId_IsIdempotent()
    {
        var account = Account.Create("acct-1");
        var tx = Transaction.Create("evt-1", "acct-1", TransactionType.CREDIT, 100m, "USD", DateTimeOffset.UtcNow);

        var firstApplied = account.ApplyTransaction(tx);
        var secondApplied = account.ApplyTransaction(tx); // same EventId again

        Assert.True(firstApplied);
        Assert.False(secondApplied);
        Assert.Equal(100m, account.Balance); // not 200
    }

    [Fact]
    public void ApplyTransaction_OutOfOrderArrival_BalanceStillCorrect()
    {
        var account = Account.Create("acct-1");
        var later = Transaction.Create("evt-2", "acct-1", TransactionType.DEBIT, 20m, "USD", new DateTimeOffset(2026, 5, 15, 14, 0, 0, TimeSpan.Zero));
        var earlier = Transaction.Create("evt-1", "acct-1", TransactionType.CREDIT, 100m, "USD", new DateTimeOffset(2026, 5, 15, 10, 0, 0, TimeSpan.Zero));

        // "later" event arrives first, "earlier" event arrives second
        account.ApplyTransaction(later);
        account.ApplyTransaction(earlier);

        Assert.Equal(80m, account.Balance); // order of arrival doesn't matter
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Transaction_Create_InvalidAmount_Throws(decimal amount)
    {
        Assert.Throws<InvalidTransactionException>(() =>
            Transaction.Create("evt-1", "acct-1", TransactionType.CREDIT, amount, "USD", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Transaction_Create_MissingEventId_Throws()
    {
        Assert.Throws<InvalidTransactionException>(() =>
            Transaction.Create("", "acct-1", TransactionType.CREDIT, 10m, "USD", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Account_Create_MissingAccountId_Throws()
    {
        Assert.Throws<InvalidTransactionException>(() => Account.Create(""));
    }
}