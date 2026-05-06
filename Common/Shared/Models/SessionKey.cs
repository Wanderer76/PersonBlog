using Shared.Services;

namespace Shared.Models;

public sealed class SessionKey(Guid id) : ICacheKey
{
    public const string Key = "SessionId";
    public string GetKey() => $"{Key}:{id}";
}
