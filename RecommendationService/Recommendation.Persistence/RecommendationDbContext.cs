using Microsoft.EntityFrameworkCore;
using Recommendation.Domain.Entities;
using Shared.Persistence;

namespace Recommendation.Persistence;

public sealed class RecommendationDbContext : BaseDbContext
{
    public const string Schema = "Recommendation";

    public DbSet<PostSnapshot> PostSnapshots => Set<PostSnapshot>();
    public DbSet<PostCategory> PostCategories => Set<PostCategory>();
    public DbSet<UserInteraction> UserInteractions => Set<UserInteraction>();
    public DbSet<UserCategoryAffinity> UserCategoryAffinities => Set<UserCategoryAffinity>();
    public DbSet<UserBlogAffinity> UserBlogAffinities => Set<UserBlogAffinity>();
    public DbSet<UserSubscription> UserSubscriptions => Set<UserSubscription>();
    public DbSet<RecommendationImpression> RecommendationImpressions => Set<RecommendationImpression>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    public RecommendationDbContext(DbContextOptions<RecommendationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RecommendationDbContext).Assembly);
    }
}
