using Microsoft.EntityFrameworkCore;
using Notification.Domain.Entities;
using Shared.Persistence;

namespace Notification.Persistence;

public sealed class NotificationDbContext(DbContextOptions<NotificationDbContext> options) : BaseDbContext(options)
{
    public DbSet<UserNotification> Notifications => Set<UserNotification>();
    public DbSet<NotificationType> NotificationTypes => Set<NotificationType>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("Notification");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NotificationDbContext).Assembly);
    }
}
