using MessageBus.Configs;
using MessageBus.EventHandler;
using MessageBus.Internal;
using MessageBus.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MessageBus
{
    public static class MessageBusServiceExtensions
    {
        public static IMessageBusBuilder AddMessageBus(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<RabbitMqMessageBus>();
            services.AddSingleton<IMessagePublish, RabbitMqMessageBus>(x => x.GetRequiredService<RabbitMqMessageBus>());
            services.AddOptions<MessageBusSubscriptionInfo>().Configure(x => new MessageBusSubscriptionInfo([]));
            services.AddSingleton<RabbitMqConnection>(configuration.GetSection("RabbitMQ:Connection").Get<RabbitMqConnection>()!);
            services.AddHostedService<DefaultHostedService>();
            return new MessageBusBuilder(services);
        }

        public static IMessageBusBuilder AddConnectionConfig<TConfig>(this IMessageBusBuilder builder, TConfig section)
            where TConfig : class
        {
            builder.Services.AddSingleton<TConfig>(section);
            return builder;
        }

        public static IMessageBusBuilder AddSubscription<TEvent, THandle>(this IMessageBusBuilder builder, Action<QueueParams> queue)
            where TEvent : class
            where THandle : class, IEventHandler<TEvent>
        {
            builder.Services.AddKeyedScoped<IEventHandler<TEvent>, THandle>(typeof(TEvent).Name);
            builder.Services.PostConfigure<MessageBusSubscriptionInfo>(sp =>
            {
                sp.AddSubscription<TEvent>(queue);
            });

            return builder;
        }
    }

    file class MessageBusBuilder : IMessageBusBuilder
    {
        private readonly IServiceCollection _services;

        public MessageBusBuilder(IServiceCollection services)
        {
            _services = services;
        }

        public IServiceCollection Services => _services;
    }
}
