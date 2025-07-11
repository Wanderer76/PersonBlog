﻿namespace MessageBus.Models
{
    internal sealed class MessageBusSubscriptionInfo
    {
        public IReadOnlyDictionary<string, Type> EventTypes { get => _eventTypes; }

        private readonly Dictionary<string, Type> _eventTypes = [];

        public readonly List<HandlerInfo> Handlers = new List<HandlerInfo>();

        public MessageBusSubscriptionInfo() { }

        public MessageBusSubscriptionInfo(Dictionary<string, Type> eventTypes)
        {
            _eventTypes = eventTypes;
        }

        public void AddSubscription<TEvent>(Action<QueueParams>? cfg)
        {
            var type = typeof(TEvent);
            _eventTypes.Add(type.Name, type);
            var queueOptions = new QueueParams();
            cfg?.Invoke(queueOptions);
            Handlers.Add(new HandlerInfo
            {
                HandlerType = type,
                Queue = queueOptions
            });
        }

        public void AddMessageInfo<TMessage>(Action<MessageInfo<TMessage>>? cfg)
        {

        }
    }

    public class MessageInfo<T>
    {
        public required T Type { get; init; }
        public string CorrelationId { get => func?.Invoke(Type); }
        public string RoutingKey { get; set; }
        public string Exchange { get; set; }

        private Func<T, string> func;

        public void UseCorrelationId(Func<T, string> message)
        {
            func = message;
        }

    }

    public class HandlerInfo
    {
        public required Type HandlerType { get; init; }
        public required QueueParams Queue { get; init; }
        public object Options { get; init; }
    }

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
}
