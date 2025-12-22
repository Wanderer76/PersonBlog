using MessageBus;
using Shared.Services;

namespace Notification.Contract.Events
{
    [EventPublish(Exchange = "notifications", RoutingKey = "create")]
    public sealed class CreateNotificationEvent
    {
        public Guid UserId { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeService.Now();
        public string Payload { get; set; }
    }
}
