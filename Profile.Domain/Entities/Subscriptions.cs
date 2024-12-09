using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Profile.Domain.Entities
{
    public class Subscriptions : IProfileEntity
    {
        [Key]
        public Guid Id { get; set; }
        public Guid ProfileId { get; set; }

        public Guid BlogId { get; set; }

        public DateTimeOffset SubscriptionStartDate { get; set; }
        public DateTimeOffset? SubscriptionEndDate { get; set; }

        [ForeignKey(nameof(ProfileId))]
        public required AppProfile Profile { get; set; }
        
        [ForeignKey(nameof(BlogId))]
        public required Blog Blog { get; set; }

    }
}
