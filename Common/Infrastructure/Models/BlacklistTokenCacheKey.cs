using Shared.Services;

namespace Infrastructure.Models
{
    public sealed class BlacklistTokenCacheKey : ICacheKey
    {
        public const string Key = nameof(BlacklistTokenCacheKey);

        private readonly Guid userId;

        public BlacklistTokenCacheKey(Guid userId)
        {
            this.userId = userId;
        }

        public string GetKey() => $"{Key}:{userId}";
    }
}
