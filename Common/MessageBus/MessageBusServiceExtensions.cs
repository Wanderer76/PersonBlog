using MessageBus.Configs;
using MessageBus.EventHandler;
using MessageBus.Internal;
using MessageBus.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace MessageBus;

public static class MessageBusServiceExtensions
{
    public static IMessageBusBuilder AddRabbitMqMessageBus(this IServiceCollection services, RabbitMqConnection configuration)
    {
        services.AddSingleton<RabbitMqMessageBus>();
        services.AddSingleton<IMessagePublish>(x => x.GetRequiredService<RabbitMqMessageBus>());
        services.AddSingleton<IMessageSubscriber>(sp => sp.GetRequiredService<RabbitMqMessageBus>());

        var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes())
             .Where(x => Attribute.IsDefined(x, typeof(EventPublishAttribute)))
             .ToList();

        services.AddSingleton<IRequestClient, RabbitMqRequestClient>();
        services.AddOptions<MessageBusInfoContainer>().PostConfigure(x => x.Init(types.Select(x => (x, x.GetCustomAttribute<EventPublishAttribute>()))));

        services.AddSingleton<RabbitMqConnection>(configuration);
        services.AddHostedService<DefaultHostedService>();
        return new MessageBusBuilder(services);
    }

    public static IMessageBusBuilder AddKafkaMessageBus(
       this IServiceCollection services,
       Action<KafkaConnection> configureConnection)
    {
        var connection = new KafkaConnection();
        configureConnection(connection);
        var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes())
          .Where(x => Attribute.IsDefined(x, typeof(EventPublishAttribute)))
          .ToList();
        services.AddOptions<MessageBusInfoContainer>().PostConfigure(x => x.Init(types.Select(x => (x, x.GetCustomAttribute<EventPublishAttribute>()))));

        services.AddSingleton(connection);
        services.AddSingleton<IMessagePublish, KafkaMessageBus>();
        services.AddSingleton<IMessageSubscriber>(sp => sp.GetRequiredService<KafkaMessageBus>());
        services.AddHostedService<DefaultHostedService>();

        return new MessageBusBuilder(services);
    }

    public static IMessageBusBuilder AddSubscription<TEvent, THandle>(this IMessageBusBuilder builder, Action<QueueParams> cfg)
        where TEvent : class
        where THandle : class, IEventHandler<TEvent>
    {
        builder.Services.AddKeyedScoped<IEventHandler<TEvent>, THandle>(typeof(TEvent).Name);
        builder.Services.PostConfigure<MessageBusInfoContainer>(sp =>
        {
            sp.AddSubscription<TEvent>(cfg);
        });

        return builder;
    }

    public static IMessageBusBuilder AddMessage<TEvent>(this IMessageBusBuilder builder, Action<MessageInfo<TEvent>> cfg)
      where TEvent : class
    {
        builder.Services.PostConfigure<MessageBusInfoContainer>(sp =>
        {
            sp.AddMessageInfo(cfg);
        });

        return builder;
    }
}

file class MessageBusBuilder(IServiceCollection services) : IMessageBusBuilder
{
    public IServiceCollection Services => services;
}
