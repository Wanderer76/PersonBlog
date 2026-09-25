using Infrastructure.Interface;
using Microsoft.EntityFrameworkCore;

namespace Notification.Persistence;

internal sealed class NotificationDbInitializer(NotificationDbContext context) : IDbInitializer
{
    public void Initialize()
    {
        context.Database.Migrate();
    }
}
