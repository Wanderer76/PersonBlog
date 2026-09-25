using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Recommendation.Domain.Entities;

namespace Recommendation.Persistence.Configurations;

internal sealed class RecommendationImpressionConfiguration : IEntityTypeConfiguration<RecommendationImpression>
{
    public void Configure(EntityTypeBuilder<RecommendationImpression> builder)
    {
        builder.ToTable("RecommendationImpressions", table => table.HasCheckConstraint(
            "CK_RecommendationImpressions_Subject",
            "(\"UserId\" IS NOT NULL AND \"AnonymousSessionId\" IS NULL) OR " +
            "(\"UserId\" IS NULL AND \"AnonymousSessionId\" IS NOT NULL)"));
        builder.HasKey(x => new { x.RequestId, x.PostId });
        builder.Property(x => x.AnonymousSessionId).HasMaxLength(200);
        builder.Property(x => x.AlgorithmVersion).HasMaxLength(100);
        builder.Property(x => x.CandidateSource).HasConversion<string>().HasMaxLength(32);
        builder.HasIndex(x => new { x.UserId, x.ShownAt });
        builder.HasIndex(x => new { x.AnonymousSessionId, x.ShownAt });
        builder.HasIndex(x => new { x.PostId, x.ShownAt });
    }
}
