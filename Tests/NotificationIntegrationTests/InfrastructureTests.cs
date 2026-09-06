using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Infrastructure.Interface;
using Microsoft.Extensions.DependencyInjection;
using Shared.Services;
using Shared.Persistence;
using Notification.Domain.Entities;
using Notification.Infrastructure;
using Notification.Persistence;

namespace NotificationIntegrationTests;

// These tests verify EF metadata and DI without opening a database connection.
public sealed class InfrastructureTests
{
    [Fact]
    public void PersistenceModelHasExplicitKeysAndNoShadowForeignKeys()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:NotificationDbContext"] = "Host=localhost;Database=notification_model_test"
        }).Build();
        services.AddNotificationPersistence(configuration);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
        var model = context.Model;

        Assert.Equal("Notification", model.GetDefaultSchema());
        var notification = model.FindEntityType(typeof(UserNotification))!;
        Assert.Equal("Notifications", notification.GetTableName());
        var link = model.FindEntityType(typeof(UserNotificationTypes))!;
        Assert.Equal(new[] { "UserNotificationId", "NotificationTypeId" },
            link.FindPrimaryKey()!.Properties.Select(property => property.Name));
        Assert.Equal(2, link.GetForeignKeys().Count());
        Assert.All(link.GetForeignKeys().SelectMany(key => key.Properties),
            property => Assert.False(property.IsShadowProperty()));
        Assert.Contains("CREATE TABLE", context.Database.GenerateCreateScript());
        var historyScript = context.GetService<IHistoryRepository>().GetCreateScript();
        Assert.Contains("\"Notification\".\"_Notification_MigrationsHistory\"", historyScript);
        Assert.IsAssignableFrom<BaseDbContext>(context);
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IDbInitializer>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IReadRepository<INotificationEntity>>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IWriteRepository<INotificationEntity>>());
        var repository = scope.ServiceProvider.GetRequiredService<IReadWriteRepository<INotificationEntity>>();
        var entity = UserNotification.Create(Guid.NewGuid(), Guid.NewGuid(), "payload", [], DateTimeOffset.UtcNow);
        repository.Add(entity);
        Assert.Equal(EntityState.Added, context.Entry(entity).State);
    }

    [Fact]
    public void DateTimeManagerUsesTheRegisteredTimeProvider()
    {
        var expected = new DateTimeOffset(2026, 9, 6, 0, 0, 0, TimeSpan.Zero);
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(new FixedTimeProvider(expected));
        services.AddNotificationInfrastructure();
        using var provider = services.BuildServiceProvider();

        Assert.Equal(expected, provider.GetRequiredService<IDateTimeManager>().UtcNow());
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
