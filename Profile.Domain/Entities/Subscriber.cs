using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Blog.Domain.Entities
{
    public class Subscriber : IBlogEntity
    {
        [Key]
        public required Guid Id { get; set; }
        public required Guid UserId { get; set; }
        public required Guid BlogId { get; set; }

        public DateTimeOffset SubscriptionStartDate { get; set; }
        public DateTimeOffset? SubscriptionEndDate { get; set; }

        [ForeignKey(nameof(BlogId))]
        public PersonBlog Blog { get; set; }
    }

    public static class SubscriptionsQueryFilters
    {
        public static IQueryable<Subscriber> Active(this IQueryable<Subscriber> query) => query.Where(x => x.SubscriptionEndDate == null);
        public static IQueryable<Subscriber> ByUserId(this IQueryable<Subscriber> query, Guid userId) => query.Where(x => x.UserId == userId);
    }
}
