using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Profile.Domain.Entities
{
    public class Subscription : IProfileEntity
    {
        [Key]
        public required Guid Id { get; set; }
        public required Guid ProfileId { get; set; }

        public required Guid BlogId { get; set; }

        public DateTimeOffset SubscriptionStartDate { get; set; }
        public DateTimeOffset? SubscriptionEndDate { get; set; }

        [ForeignKey(nameof(ProfileId))]
        public AppProfile Profile { get; set; }

        [ForeignKey(nameof(BlogId))]
        public Blog Blog { get; set; }
    }

    public static class SubscriptionsQueryFilters
    {
        public static IQueryable<Subscription> Active(this IQueryable<Subscription> query) => query.Where(x => x.SubscriptionEndDate == null);
        public static IQueryable<Subscription> ByUserId(this IQueryable<Subscription> query, Guid userId) => query.Where(x => x.Profile.UserId == userId);
    }
}
