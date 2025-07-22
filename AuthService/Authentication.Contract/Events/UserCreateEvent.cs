using MessageBus;

namespace Authentication.Contract.Events;

[EventPublish(Exchange = "user-events", RoutingKey = "user.create")]
public class UserCreateEvent
{
    public Guid UserId { get; set; }
    public string UserName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string PhotoUrl { get; set; }
}
