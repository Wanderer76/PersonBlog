using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Notification.Persistence;

/// <summary>Used by dotnet ef. Generating migrations does not require a live database.</summary>
public sealed class NotificationDbContextFactory : IDesignTimeDbContextFactory<NotificationDbContext>
{
    public NotificationDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("NOTIFICATION_MIGRATIONS_CONNECTION")
            ?? "Host=localhost;Database=notification";
        var options = new DbContextOptionsBuilder<NotificationDbContext>()
            .UseNpgsql(connection, provider => provider.MigrationsHistoryTable("_Notification_MigrationsHistory", "Notification"))
            .Options;
        return new NotificationDbContext(options);
    }
}
