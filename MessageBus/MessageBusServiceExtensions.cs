using MessageBus.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace MessageBus
{
    public static class MessageBusServiceExtensions
    {
        public static void AddMessageBus(this IServiceCollection services)
        {
            services.AddSingleton<IMessageBus, RabbitMqMessageBus>();
        }
    }
}
