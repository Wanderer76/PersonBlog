namespace MessageBus.EventHandler
{
    public interface IEventHandler
    {
        Task Handle(object @event);
    }

    public interface IEventHandler<TEvent> : IEventHandler
    {
        Task Handle(TEvent @event);
        Task IEventHandler.Handle(object @event) => Handle((TEvent)@event);
    }
}
