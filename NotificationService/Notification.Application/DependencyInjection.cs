using Microsoft.Extensions.DependencyInjection;
using Notification.Application.Fanout;
using Notification.Application.Notifications;
using Notification.Application.Preferences;

namespace Notification.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateNotification>();
        services.AddScoped<GetNotifications>();
        services.AddScoped<CountUnread>();
        services.AddScoped<MarkRead>();
        services.AddScoped<GetNotificationPreferences>();
        services.AddScoped<UpdateNotificationPreferences>();
        services.AddScoped<StartCampaign>();
        services.AddScoped<ProcessRecipientBatch>();
        return services;
    }
}
