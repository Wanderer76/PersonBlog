using MessageBus;

namespace Authentication.Contract.Events
{
    [EventPublish(Exchange = "user-events", RoutingKey = "profile.register")]
    public class ProfileRegisterEvent
    {
        public string? Name { get; set; }
        public DateTimeOffset? Birthdate { get; set; }
        public Guid UserId { get; set; }
        public string Email { get; set; }
        public DateTimeOffset CreatedAt { get; set; }

        public ProfileRegisterEvent(string? name, DateTimeOffset? birthdate, Guid userId, string email, DateTimeOffset createdAt)
        {
            Name = name;
            Birthdate = birthdate;
            UserId = userId;
            Email = email;
            CreatedAt = createdAt;
        }
    }
}
