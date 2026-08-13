using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Recommendation.Domain.Entities;

namespace Recommendation.Persistence.Configurations;

internal sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("InboxMessages");
        builder.HasKey(x => x.EventId);
        builder.Property(x => x.EventType).HasMaxLength(200);
        builder.Property(x => x.LastError).HasMaxLength(4000);
        builder.Ignore(x => x.IsProcessed);
        builder.HasIndex(x => new { x.ProcessedAt, x.ReceivedAt });
    }
}
