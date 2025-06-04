using Infrastructure.Models;
namespace MessageBus.EventHandler;

public interface IEventHandler
{
}

public interface IEventHandler<in TEvent> : IEventHandler
{
    Task Handle(IMessageContext<TEvent> @event);
}

public static class MessageContext
{
    public static IMessageContext<T> Create<T>(Guid? correlationId, T message, IMessagePublish messagePublish)
    {
        return new MessageContext<T>(correlationId, message, messagePublish);
    }
}
public interface IMessageContext<out TMessage> : IMessagePublish
{
    public Guid? CorrelationId { get; }
    public TMessage Message { get; }
}

internal sealed class MessageContext<TMessage> : IMessageContext<TMessage>
{
    public Guid? CorrelationId { get; }
    public TMessage Message { get; }

    private readonly IMessagePublish _publish;

    internal MessageContext(Guid? correlationId, TMessage message, IMessagePublish publish)
    {
        CorrelationId = correlationId;
        Message = message;
        _publish = publish;
    }

    public Task PublishAsync<T>(string exchangeName, string routingKey, T message, MessageProperty? cfg = null)
    {
        return _publish.PublishAsync(exchangeName, routingKey, message, cfg);
    }

    public Task PublishAsync<T>(T message, MessageProperty? cfg = null)
    {
        return _publish.PublishAsync(message, cfg);
    }
}
