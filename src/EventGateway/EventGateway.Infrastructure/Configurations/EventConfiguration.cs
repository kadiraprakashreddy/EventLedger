using EventGateway.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventGateway.Infrastructure.Data.Configurations;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.HasKey(e => e.EventId);              // PK itself enforces idempotency at DB level
        builder.Property(e => e.Amount).HasColumnType("decimal(18,2)");
        builder.HasIndex(e => new { e.AccountId, e.EventTimestamp }); // ordered listing per account
    }
}