namespace MessageBus.Models;

internal sealed class MessageBusInfoContainer
{
    private Dictionary<string, HandlerInfo> _handlerTypes = [];
    private Dictionary<string, EventSendParams> _eventTypes = [];

    public IReadOnlyDictionary<string, HandlerInfo> HandlerTypes { get => _handlerTypes; }
    public IReadOnlyDictionary<string, EventSendParams> Events { get => _eventTypes; }

    public MessageBusInfoContainer() { }

    public void AddSubscription<TEvent>(Action<QueueParams>? cfg)
    {
        var type = typeof(TEvent);
        var queueOptions = new QueueParams();
        cfg?.Invoke(queueOptions);
        _handlerTypes.TryAdd(type.Name, new HandlerInfo(type, queueOptions));
    }

    public void AddMessageInfo<TMessage>(Action<MessageInfo<TMessage>>? cfg)
    {
        var type = typeof(TMessage);
        var queueOptions = new MessageInfo<TMessage>();
        cfg?.Invoke(queueOptions);
        _eventTypes.Add(type.Name, new EventSendParams(type, queueOptions.RoutingKey, queueOptions.Exchange, queueOptions.Correlation));
    }

    internal void Init(IEnumerable<(Type, EventPublishAttribute)> types)
    {
        _eventTypes = types
            .ToDictionary(x => x.Item1.Name, x => new EventSendParams(x.Item1, x.Item2.RoutingKey, x.Item2.Exchange, null));
    }
}

public sealed class MessageInfo<T>
{
    public Func<string> Correlation { get; private set; }
    public string RoutingKey { get; set; }
    public string Exchange { get; set; }

    public void UseCorrelationId(Func<string> message) => Correlation = message;
}

public sealed record EventSendParams(Type Type, string RoutingKey, string Exchange, Func<string>? CorrelationFunc)
{
    public string? CorrelationId { get => CorrelationFunc?.Invoke(); }
}

public sealed record HandlerInfo(Type HandlerType, QueueParams Queue);

public class QueueParams
{
    public string QueueName { get; set; }
    public bool Durable { get; set; } = true;
    public bool Exclusive { get; set; }
    public bool AutoDelete { get; set; }
    public int PrefetchCount { get; set; } = 10; // количество сообщений, которые можно обрабатывать одновременно
    //public int RetryCount { get; set; } = 3; // количество повторных попыток
    public ExchangeParam Exchange { get; set; }
}

public class ExchangeParam
{
    public string Name { get; set; }
    public string RoutingKey { get; set; }
    public string ExchangeType { get; set; } = "direct";
    public bool Durable { get; set; } = true;
    public bool AutoDelete { get; set; }
}
