using Blog.Contracts.Events;
using MessageBus;
using MessageBus.Configs;
using MessageBus.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shared.Services;
using Notification.Infrastructure.Messaging;
using Notification.Infrastructure.Services;

namespace Notification.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationInfrastructure(this IServiceCollection services)
    {
        services.TryAddSingleton<TimeProvider>(TimeProvider.System);
        services.TryAddSingleton<IDateTimeManager, SystemDateTimeManager>();
        return services;
    }

    // Transitional adapter, not a complete notification delivery pipeline.
    public static IServiceCollection AddLegacyPostNotifications(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient("Blog", client =>
        {
            client.BaseAddress = new Uri(configuration["AppUrls:Blog"]
                ?? throw new InvalidOperationException("AppUrls:Blog is required."));
            client.Timeout = TimeSpan.FromSeconds(1);
        });
        services.AddRabbitMqMessageBus(configuration.GetSection("RabbitMQ:Connection").Get<RabbitMqConnection>()
                ?? throw new InvalidOperationException("RabbitMQ:Connection is required."))
            .AddSubscription<PostUpdateEvent, PostCreateEventHandler>(options =>
            {
                options.QueueName = "post-create-notifications";
                options.Exchange = new ExchangeParam { Name = "post-update", ExchangeType = "fanout" };
            });
        return services;
    }
}
