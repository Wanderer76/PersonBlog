namespace MessageBus.EventHandler;

public interface IEventHandler
{
    Task Handle(MessageContext<object> @event);
}

public interface IEventHandler<TEvent> : IEventHandler
{
    Task Handle(MessageContext<TEvent> @event);
    Task IEventHandler.Handle(MessageContext<object> @event) => Handle(@event);
}

public sealed class MessageContext<TMessage>
{
    public Guid? CorrelationId { get; set; }
    public TMessage Message { get; set; }

    public MessageContext(Guid? correlationId, TMessage message)
    {
        CorrelationId = correlationId;
        Message = message;
    }

    public static implicit operator MessageContext<TMessage>(MessageContext<object> t)
    {
        return new(t.CorrelationId, (TMessage)t.Message);
    }
}
