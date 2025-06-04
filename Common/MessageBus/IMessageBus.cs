using Infrastructure.Models;

namespace MessageBus
{
    public interface IMessagePublish
    {
        //Task SendMessageAsync<T>(string exchangeName, string routingKey, T message) where T : BaseEvent;
        Task PublishAsync<T>(string exchangeName, string routingKey, T message, MessageProperty? cfg = null);
        Task PublishAsync<T>(T message, MessageProperty? cfg = null);
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public class EventPublishAttribute : Attribute
    {
        public string Exchange { get; set; }
        public string RoutingKey { get; set; }
    }

    public class MessageProperty
    {
        public bool Persistence { get; set; } = true;
        public string CorrelationId { get; set; }
        public string RoutingKey { get; set; }
        public string Exchange { get; set; }
    }
}