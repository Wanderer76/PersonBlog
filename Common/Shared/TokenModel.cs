using System.Text.Json.Serialization;

namespace Shared
{
    public class TokenModel
    {
        public Guid Id { get; set; }
        public string Login { get; set; }
        public Guid UserId { get; set; }
        [JsonPropertyName("nbf")]
        public DateTimeOffset CreatedAt { get; set; }
        [JsonPropertyName("exp")]
        public DateTimeOffset ExpiredAt { get; set; }
        public string Type { get; set; }
        public Guid RoleId { get; set; }
        public string? ContextType { get; set; }
        public Guid ContextId { get; set; }

        /// <summary>Compatibility alias for the active <c>blog</c> context.</summary>
        public Guid BlogId
        {
            get => ContextType == Models.UserContextTypes.Blog ? ContextId : Guid.Empty;
            set
            {
                if (value == Guid.Empty)
                    return;

                ContextType = Models.UserContextTypes.Blog;
                ContextId = value;
            }
        }
    }
}
