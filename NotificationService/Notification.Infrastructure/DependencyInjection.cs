using Blog.Contracts.Events;
using MessageBus;
using MessageBus.Configs;
using MessageBus.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.SignalR;
using Notification.Application.Abstractions;
using Notification.Infrastructure.Delivery;
using Shared.Services;
using Notification.Infrastructure.Messaging;
using Notification.Infrastructure.Services;
using Notification.Infrastructure.Clients;
using Notification.Infrastructure.BackgroundJobs;
using Notification.Application;

namespace Notification.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationInfrastructure(this IServiceCollection services)
    {
        services.AddNotificationApplication();
        services.TryAddSingleton<TimeProvider>(TimeProvider.System);
        services.TryAddSingleton<IDateTimeManager, SystemDateTimeManager>();
        services.AddHttpClient<IRecipientDirectory, BlogRecipientDirectory>((provider, client) =>
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            client.BaseAddress = new Uri(configuration["AppUrls:NotificationBlog"]
                ?? throw new InvalidOperationException("AppUrls:NotificationBlog is required."));
            client.Timeout = TimeSpan.FromSeconds(15);
        });
        services.AddSignalR();
        services.TryAddSingleton<IUserIdProvider, NotificationUserIdProvider>();
        services.TryAddEnumerable(
            ServiceDescriptor.Scoped<INotificationDelivery, InAppNotificationDelivery>());
        return services;
    }

    /// <summary>Enable only after applying Notification.Persistence migrations.</summary>
    public static IServiceCollection AddNotificationWorkers(this IServiceCollection services,
        Action<NotificationWorkerOptions>? configure = null)
    {
        var options = services.AddOptions<NotificationWorkerOptions>();
        if (configure is not null) options.Configure(configure);
        options.Validate(x => x.PollInterval > TimeSpan.Zero && x.LeaseDuration > TimeSpan.Zero &&
            x.LeaseDuration <= TimeSpan.FromHours(1) && x.RetryDelay >= TimeSpan.Zero &&
            x.RetryDelay <= TimeSpan.FromDays(1) && x.MaxAttempts > 0 && x.FanoutPageSize is >= 1 and <= 500,
            "Invalid notification worker settings.").ValidateOnStart();
        services.AddScoped<NotificationWorkProcessor>();
        services.AddHostedService<NotificationWorker>();
        return services;
    }

    public static IServiceCollection AddNotificationPushDelivery<TSender>(this IServiceCollection services)
        where TSender : class, IPushNotificationSender
    {
        services.TryAddScoped<IPushNotificationSender, TSender>();
        services.TryAddEnumerable(
            ServiceDescriptor.Scoped<INotificationDelivery, PushNotificationDelivery>());
        return services;
    }

    public static IMessageBusBuilder AddPostPublishedNotifications(this IMessageBusBuilder builder)
    {
        return builder.AddSubscription<PostPublishedV1, PostPublishedV1Handler>(options =>
        {
            options.QueueName = PostPublishedV1Handler.ConsumerName;
            options.PrefetchCount = 10;
            options.Exchange = new ExchangeParam
            {
                Name = BlogIntegrationEvents.Exchange,
                RoutingKey = BlogIntegrationEvents.PostPublishedV1RoutingKey,
                ExchangeType = "direct"
            };
        });
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
