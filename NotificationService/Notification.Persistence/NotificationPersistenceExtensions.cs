using Infrastructure.Extensions;
using Infrastructure.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notification.Domain.Entities;

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
    }
}
