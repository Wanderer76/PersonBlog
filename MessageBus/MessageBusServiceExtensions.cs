using MessageBus.Configs;
using MessageBus.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MessageBus
{
    public static class MessageBusServiceExtensions
    {
        public static void AddMessageBus(this IServiceCollection services, IConfiguration configuration, Dictionary<string, Type>? info = null)
        {
            services.AddSingleton<RabbitMqMessageBus>();
            services.AddSingleton<RabbitMqConnection>(configuration.GetSection("RabbitMQ:Connection").Get<RabbitMqConnection>()!);
            services.AddSingleton<RabbitMqUploadVideoConfig>(configuration.GetSection("RabbitMQ:UploadVideoConfig").Get<RabbitMqUploadVideoConfig>()!);
            services.AddSingleton<MessageBusSubscriptionInfo>(new MessageBusSubscriptionInfo(info ?? []));
        }
    }
}
