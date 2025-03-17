namespace Shared.Models
{
    public readonly struct SessionKey
    {
        public const string Key = "sessionId";

        private readonly Guid sessionId;

        public SessionKey(Guid id)
        {
            sessionId = id;
        }

        public SessionKey(string id)
        {
            sessionId = Guid.Parse(id);
        }

        public static implicit operator string(SessionKey SessionKey) => $"{SessionKey.Key}:{SessionKey.sessionId}";
    }
}
