using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Recommendation.Domain.Entities;

namespace Recommendation.Persistence.Configurations;

internal sealed class UserInteractionConfiguration : IEntityTypeConfiguration<UserInteraction>
{
    public void Configure(EntityTypeBuilder<UserInteraction> builder)
    {
        builder.ToTable("UserInteractions", table => table.HasCheckConstraint(
            "CK_UserInteractions_Subject",
            "(\"UserId\" IS NOT NULL AND \"AnonymousSessionId\" IS NULL) OR " +
            "(\"UserId\" IS NULL AND \"AnonymousSessionId\" IS NOT NULL)"));
        builder.HasKey(x => x.EventId);
        builder.Property(x => x.AnonymousSessionId).HasMaxLength(200);
        builder.Property(x => x.InteractionType).HasConversion<string>().HasMaxLength(32);
        builder.HasIndex(x => new { x.UserId, x.OccurredAt });
        builder.HasIndex(x => new { x.AnonymousSessionId, x.OccurredAt });
        builder.HasIndex(x => new { x.PostId, x.OccurredAt });
    }
}
