using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageBus.Models
{
    public sealed class MessageBusSubscriptionInfo
    {
        public readonly Dictionary<string, Type> EventTypes = [];

        public MessageBusSubscriptionInfo() { }

        public MessageBusSubscriptionInfo(Dictionary<string, Type> eventTypes)
        {
            EventTypes = eventTypes;
        }
        public void AddSubscription(string eventType, Type handlerType)
        {
            EventTypes.Add(eventType, handlerType);
        }
    }
}
