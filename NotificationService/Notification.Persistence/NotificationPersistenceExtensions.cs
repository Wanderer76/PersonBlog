using Infrastructure.Extensions;
using Infrastructure.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notification.Domain.Entities;
using Notification.Application.Abstractions;
using Notification.Persistence.Stores;

namespace Notification.Persistence;

public static class NotificationPersistenceExtensions
{
    public static void AddNotificationPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(nameof(NotificationDbContext));
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddNpgSqlDbContext<NotificationDbContext>(connectionString);
        services.AddScoped<IDbInitializer, NotificationDbInitializer>();
        services.AddDefaultRepository<NotificationDbContext, INotificationEntity>();
        services.AddScoped<StoreOperation>();
        services.AddScoped<INotificationStore, NotificationStore>();
        services.AddScoped<INotificationPreferenceStore, NotificationPreferenceStore>();
        services.AddScoped<IFanoutStore, FanoutStore>();
        services.AddScoped<INotificationWorkStore, NotificationWorkStore>();
    }
}
