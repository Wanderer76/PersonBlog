using MessageBus.Models;
using Shared.Services;

namespace MessageBus.EventHandler;

public interface IEventHandler<in TEvent>
{
    Task Handle(IMessageContext<TEvent> @event);
}

public interface IReplyContext
{
    Task ReplyAsync<TResponse>(TResponse response);
}

public static class MessageContext
{
    public static IMessageContext<T> Create<T>(Guid? correlationId, T message, IMessagePublish messagePublish)
    {
        return new MessageContext<T>(correlationId, message, messagePublish, null, null, null);
    }

    public static IMessageContext<T> CreateForReply<T>(Guid? correlationId, T message, IMessagePublish messagePublish, string replyTo, string requestCorrelationId, IRequestClient requestClient)
    {
        return new MessageContext<T>(correlationId, message, messagePublish, replyTo, requestCorrelationId, requestClient);
    }
}

public interface IMessageContext<out TMessage> : IMessagePublish, IReplyContext
{
    public Guid? CorrelationId { get; }
    public TMessage Message { get; }
}

file sealed class MessageContext<TMessage> : IMessageContext<TMessage>
{
    public Guid? CorrelationId { get; }
    public TMessage Message { get; }
    private readonly IMessagePublish _publish;
    private readonly string? _replyTo;
    private readonly string? _requestCorrelationId;
    private readonly IRequestClient requestClient;

    IRequestClient IHave<IRequestClient>.Value => requestClient;

    internal MessageContext(Guid? correlationId, TMessage message, IMessagePublish publish, string? replyTo, string? requestCorrelationId, IRequestClient requestClient)
    {
        CorrelationId = correlationId;
        Message = message;
        _publish = publish;
        _replyTo = replyTo;
        _requestCorrelationId = requestCorrelationId;
        this.requestClient = requestClient;
    }

    public Task PublishAsync<T>(BaseEvent<T> message, MessageProperty? cfg = null)
    {
        return _publish.PublishAsync(message, cfg);
    }

    public Task PublishAsync(BaseEvent message, MessageProperty? cfg = null)
    {
        return _publish.PublishAsync(message, cfg);
    }

    public Task ReplyAsync<TResponse>(TResponse response)
    {
        if (string.IsNullOrEmpty(_replyTo))
        {
            throw new InvalidOperationException("ReplyTo is not specified. The incoming message was not sent as a request.");
        }
        var baseEvent = BaseEvent<TResponse>.Create(response);
        var cfg = new MessageProperty
        {
            Exchange = "",
            RoutingKey = _replyTo,
            CorrelationId = _requestCorrelationId,
            Persistence = false
        };

        return _publish.PublishAsync(baseEvent, cfg);
    }
}
