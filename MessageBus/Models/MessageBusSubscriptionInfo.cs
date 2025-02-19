namespace MessageBus.Models
{
    public sealed class MessageBusSubscriptionInfo
    {
        public IReadOnlyDictionary<string, Type> EventTypes { get => _eventTypes; }

        private readonly Dictionary<string, Type> _eventTypes = [];

        public MessageBusSubscriptionInfo() { }

        public MessageBusSubscriptionInfo(Dictionary<string, Type> eventTypes)
        {
            _eventTypes = eventTypes;
        }
        public void AddSubscription(string eventType, Type handlerType)
        {
            _eventTypes.Add(eventType, handlerType);
        }
    }
}
