using MessageBus.Configs;
using MessageBus.EventHandler;
using MessageBus.Internal;
using MessageBus.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MessageBus;

public static class MessageBusServiceExtensions
{
    public static IMessageBusBuilder AddMessageBus(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<RabbitMqMessageBus>();
        services.AddSingleton<IMessagePublish>(x => x.GetRequiredService<RabbitMqMessageBus>());
        services.AddSingleton<IMessageSubscriber>(sp => sp.GetRequiredService<RabbitMqMessageBus>());

        var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes())
             .Where(x => Attribute.IsDefined(x, typeof(EventPublishAttribute)))
             .ToList();

        services.AddOptions<MessageBusSubscriptionInfo>().PostConfigure(x => x.Init(types));
        services.AddSingleton(configuration.GetSection("RabbitMQ:Connection").Get<RabbitMqConnection>()!);
        services.AddHostedService<DefaultHostedService>();
        return new MessageBusBuilder(services);
    }

    public static IMessageBusBuilder AddSubscription<TEvent, THandle>(this IMessageBusBuilder builder, Action<QueueParams> cfg)
        where TEvent : class
        where THandle : class, IEventHandler<TEvent>
    {
        builder.Services.AddKeyedScoped<IEventHandler<TEvent>, THandle>(typeof(TEvent).Name);
        builder.Services.PostConfigure<MessageBusSubscriptionInfo>(sp =>
        {
            sp.AddSubscription<TEvent>(cfg);
        });

        return builder;
    }
    public static IMessageBusBuilder AddMessage<TEvent>(this IMessageBusBuilder builder, Action<MessageInfo<TEvent>> cfg)
      where TEvent : class
    {
        builder.Services.PostConfigure<MessageBusSubscriptionInfo>(sp =>
        {
            sp.AddMessageInfo(cfg);
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
