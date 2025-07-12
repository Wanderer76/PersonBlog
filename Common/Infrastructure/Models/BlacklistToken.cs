using Shared.Services;

namespace Infrastructure.Models
{
    public sealed class BlacklistToken : ICacheKey
    {
        public const string Key = nameof(BlacklistToken);

        private readonly Guid userId;

        public BlacklistToken(Guid userId)
        {
            this.userId = userId;
        }

        public string GetKey() => $"{Key}:{userId}";
    }
}
