using Shared.Services;
using System.ComponentModel.DataAnnotations.Schema;

namespace Blog.Domain.Entities
{
    public class ProfileSubscription : IProfileEntity
    {
        public Guid ProfileId { get; set; }
        public Guid SubscriptionLevelId { get; set; }
        public bool IsActive { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }

        [ForeignKey(nameof(SubscriptionLevelId))]
        public SubscriptionLevel SubscriptionLevel { get; set; }

        [ForeignKey(nameof(ProfileId))]
        public AppProfile Profile { get; set; }

        private ProfileSubscription() { }

        public ProfileSubscription(Guid profileId, Guid subscriptionLevelId)
        {
            var now = DateTimeService.Now();
            ProfileId = profileId;
            SubscriptionLevelId = subscriptionLevelId;
            CreatedAt = now;
            UpdatedAt = now;
            IsActive = true;
        }
    }
}
