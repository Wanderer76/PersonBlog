using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Recommendation.Domain.Entities;

namespace Recommendation.Persistence.Configurations;

internal sealed class PostSnapshotConfiguration : IEntityTypeConfiguration<PostSnapshot>
{
    public void Configure(EntityTypeBuilder<PostSnapshot> builder)
    {
        builder.ToTable("PostSnapshots");
        builder.HasKey(x => x.PostId);
        builder.Property(x => x.SourceVersion).IsConcurrencyToken();
        builder.Property(x => x.PostType).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.Title).HasMaxLength(500);
        builder.Property(x => x.Description).HasMaxLength(4000);
        builder.Property(x => x.Visibility).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.ProcessState).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.PreviewObjectName).HasMaxLength(1000);

        builder.HasIndex(x => new { x.Visibility, x.ProcessState, x.IsDeleted, x.IsBanned });
        builder.HasIndex(x => new { x.BlogId, x.CreatedAt });
        builder.HasIndex(x => x.UpdatedAt);

        builder.HasMany(x => x.Categories)
            .WithOne(x => x.Post)
            .HasForeignKey(x => x.PostId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Categories)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(x => x.IsEligibleForPublicRecommendations);
    }
}
