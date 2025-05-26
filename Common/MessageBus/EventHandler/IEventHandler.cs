namespace MessageBus.EventHandler;

public interface IEventHandler
{
    Task Handle(MessageContext @event);
}

public interface IEventHandler<TEvent> : IEventHandler
{
    Task Handle(MessageContext<TEvent> @event);
    Task IEventHandler.Handle(MessageContext @event) => Handle(@event);
}

public class MessageContext
{
    public Guid? CorrelationId { get; set; }
    public object Message { get; set; }

    public MessageContext(Guid? correlationId, object message)
    {
        CorrelationId = correlationId;
        Message = message;
    }

    public static MessageContext<T> Create<T>(Guid? correlationId, T message)
    {
        return new MessageContext<T>(correlationId, message);
    }

    public static MessageContext<T> Create<T>(MessageContext ctx)
    {
        return Create<T>(ctx.CorrelationId, (T)ctx.Message);
    }
}


public sealed class MessageContext<TMessage>
{
    public Guid? CorrelationId { get; set; }
    public TMessage Message { get; set; }

    internal MessageContext(Guid? correlationId, TMessage message)
    {
        CorrelationId = correlationId;
        Message = message;
    }

    public static implicit operator MessageContext<TMessage>(MessageContext t)
    {
        return MessageContext.Create<TMessage>(t);
    }
}
