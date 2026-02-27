using MessageBus;

namespace Authentication.Contract.Events
{
    [EventPublish(Exchange = "user-events", RoutingKey = "profile.register")]
    public class ProfileRegisterEvent
    {
        public string? Name { get; set; }
        //public DateTimeOffset? Birthdate { get; set; }
        public Guid UserId { get; set; }
        //public string Email { get; set; }
        public DateTimeOffset CreatedAt { get; set; }

        public ProfileRegisterEvent(string? name, Guid userId, DateTimeOffset createdAt)
        {
            Name = name;
            UserId = userId;
            CreatedAt = createdAt;
        }
    }
}
