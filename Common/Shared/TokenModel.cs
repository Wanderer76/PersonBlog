using System.Text.Json.Serialization;

namespace Shared
{
    public class TokenModel
    {
        public string Login { get; set; }
        public Guid UserId { get; set; }
        [JsonPropertyName("nbf")]
        public DateTimeOffset CreatedAt { get; set; }
        [JsonPropertyName("exp")]
        public DateTimeOffset ExpiredAt { get; set; }
        public string Type { get; set; }
        public Guid RoleId { get; set; }
        public Guid BlogId {  get; set; }
    }
}
