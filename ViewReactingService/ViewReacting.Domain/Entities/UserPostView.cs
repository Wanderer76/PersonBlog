using Shared.Services;

namespace ViewReacting.Domain.Entities
{
    public class UserPostView : IUserEntity
    {
        public Guid Id { get; private set; }
        public Guid UserId { get; private set; }
        public Guid PostId { get; private set; }
        public double WatchedTime { get; set; }
        public bool IsCompleteWatch { get; set; }
        public DateTimeOffset WatchedAt { get; private set; }
        public bool IsDelete {  get; private set; }

        public UserPostView(Guid userId, Guid postId, double watchedTime, bool isCompleteWatch)
        {
            Id = GuidService.GetNewGuid();
            UserId = userId;
            PostId = postId;
            WatchedTime = watchedTime;
            IsCompleteWatch = isCompleteWatch;
            WatchedAt = DateTimeService.Now();
            IsDelete = false;
        }
    }

    public struct UserPostViewCacheKey : ICacheKey
    {
        private const string Key = nameof(UserPostView);
        private readonly Guid UserId;

        public UserPostViewCacheKey(Guid userId)
        {
            UserId = userId;
        }

        public string GetKey() => $"{Key}:{UserId}";
    }
}
