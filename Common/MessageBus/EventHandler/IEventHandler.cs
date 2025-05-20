
namespace MessageBus.EventHandler
{
    public interface IEventHandler
    {
        Task Handle(MessageContext<object> @event);
    }

    public interface IEventHandler<TEvent> : IEventHandler
    {
        Task Handle(MessageContext<TEvent> @event);
        Task IEventHandler.Handle(MessageContext<object> @event) => Handle(new MessageContext<TEvent>(@event.CorrelationId, (TEvent)@event.Message));
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
    }
}
