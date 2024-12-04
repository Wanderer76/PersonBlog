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

        public DateTimeOffset SubscriprionStartDate { get; set; }
        public DateTimeOffset? SubscriprionEndDate { get; set; }

        [ForeignKey(nameof(ProfileId))]
        public AppProfile Profile { get; set; }
    }
}
