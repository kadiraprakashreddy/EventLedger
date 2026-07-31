using EventGateway.Domain.Entities;
using EventGateway.Domain.Enums;
using EventGateway.Domain.Exceptions;
using Xunit;

namespace EventGateway.UnitTests;

public class EventTests
{
    [Fact]
    public void Create_ValidEvent_SetsReceivedStatus()
    {
        var evt = Event.Create("evt-1", "acct-1", TransactionType.CREDIT, 100m, "USD", DateTimeOffset.UtcNow, null, "trace-1");

        Assert.Equal(EventStatus.Received, evt.Status);
        Assert.Equal("evt-1", evt.EventId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Create_InvalidAmount_Throws(decimal amount)
    {
        Assert.Throws<InvalidEventException>(() =>
            Event.Create("evt-1", "acct-1", TransactionType.CREDIT, amount, "USD", DateTimeOffset.UtcNow, null, "trace-1"));
    }

    [Fact]
    public void Create_MissingEventId_Throws()
    {
        Assert.Throws<InvalidEventException>(() =>
            Event.Create("", "acct-1", TransactionType.CREDIT, 10m, "USD", DateTimeOffset.UtcNow, null, "trace-1"));
    }

    [Fact]
    public void Create_MissingAccountId_Throws()
    {
        Assert.Throws<InvalidEventException>(() =>
            Event.Create("evt-1", "", TransactionType.CREDIT, 10m, "USD", DateTimeOffset.UtcNow, null, "trace-1"));
    }

    [Fact]
    public void Create_MissingCurrency_Throws()
    {
        Assert.Throws<InvalidEventException>(() =>
            Event.Create("evt-1", "acct-1", TransactionType.CREDIT, 10m, "", DateTimeOffset.UtcNow, null, "trace-1"));
    }

    [Fact]
    public void MarkApplied_SetsStatusToApplied()
    {
        var evt = Event.Create("evt-1", "acct-1", TransactionType.CREDIT, 10m, "USD", DateTimeOffset.UtcNow, null, "trace-1");
        evt.MarkApplied();
        Assert.Equal(EventStatus.Applied, evt.Status);
    }

    [Fact]
    public void MarkFailed_SetsStatusToFailed()
    {
        var evt = Event.Create("evt-1", "acct-1", TransactionType.CREDIT, 10m, "USD", DateTimeOffset.UtcNow, null, "trace-1");
        evt.MarkFailed();
        Assert.Equal(EventStatus.Failed, evt.Status);
    }
}