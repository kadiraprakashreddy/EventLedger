using AccountService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccountService.Infrastructure.Data.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedOnAdd();

        // idempotency guard, enforced by DB
        builder.HasIndex(t => t.EventId).IsUnique();
        // for "recent transactions"
        builder.HasIndex(t => new { t.AccountId, t.EventTimestamp }); 
    }
}