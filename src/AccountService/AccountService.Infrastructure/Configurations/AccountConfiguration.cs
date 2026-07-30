using AccountService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccountService.Infrastructure.Data.Configurations;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.HasKey(a => a.AccountId);
        builder.Property(a => a.Balance).HasColumnType("decimal(18,2)");

        builder.HasMany(a => a.Transactions)
               .WithOne()
               .HasForeignKey(t => t.AccountId);
    }
}