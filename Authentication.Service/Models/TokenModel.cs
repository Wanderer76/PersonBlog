namespace Authentication.Service.Models
{
    public class TokenModel
    {
        public Guid UserId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset ExpiredAt { get; set; }
        public string Type { get; set; }
        public IEnumerable<Guid> RoleIds { get; set; }
    }
}
