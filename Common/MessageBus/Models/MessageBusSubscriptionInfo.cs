namespace MessageBus.Models
{
    public sealed class MessageBusSubscriptionInfo
    {
        public IReadOnlyDictionary<string, Type> EventTypes { get => _eventTypes; }

        private readonly Dictionary<string, Type> _eventTypes = [];

        public readonly List<HandlerInfo> handlerInfos = new List<HandlerInfo>();

        public MessageBusSubscriptionInfo() { }

        public MessageBusSubscriptionInfo(Dictionary<string, Type> eventTypes)
        {
            _eventTypes = eventTypes;
        }

        public void AddSubscription(string eventType, Type handlerType, Action<QueueParams> queue)
        {
            _eventTypes.Add(eventType, handlerType);
            var queueOptions = new QueueParams();
            queue(queueOptions);
            handlerInfos.Add(new HandlerInfo
            {
                HanldlerType = handlerType,
                Queue = queueOptions
            });
        }
    }

    public class HandlerInfo
    {
        public Type HanldlerType { get; init; }
        public QueueParams Queue { get; init; }
        public object Options { get; init; }
    }

    public class QueueParams
    {
        public string Name {  get; set; }
        public bool Durable { get; set; } = true;
        public bool Exclusive { get; set; }
        public bool AutoDelete { get; set; }
        public ExchangeParam Exchange {  get; set; }
    }

    public class ExchangeParam
    {
        public string Name { get; set; }
        public string RoutingKey {  get; set; }
        public string ExchangeType { get; set; } = "direct";
        public bool Durable { get; set; } = true;
        public bool AutoDelete { get; set; }
    }
}
