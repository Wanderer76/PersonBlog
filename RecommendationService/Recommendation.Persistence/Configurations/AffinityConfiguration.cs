using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Recommendation.Domain.Entities;

namespace Recommendation.Persistence.Configurations;

internal sealed class UserCategoryAffinityConfiguration : IEntityTypeConfiguration<UserCategoryAffinity>
{
    public void Configure(EntityTypeBuilder<UserCategoryAffinity> builder)
    {
        builder.ToTable("UserCategoryAffinities");
        builder.HasKey(x => new { x.UserId, x.CategoryId });
        builder.HasIndex(x => new { x.UserId, x.Score });
    }
}

internal sealed class UserBlogAffinityConfiguration : IEntityTypeConfiguration<UserBlogAffinity>
{
    public void Configure(EntityTypeBuilder<UserBlogAffinity> builder)
    {
        builder.ToTable("UserBlogAffinities");
        builder.HasKey(x => new { x.UserId, x.BlogId });
        builder.HasIndex(x => new { x.UserId, x.Score });
    }
}
