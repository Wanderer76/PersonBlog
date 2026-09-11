using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Infrastructure.Interface;
using Microsoft.Extensions.DependencyInjection;
using Shared.Services;
using Shared.Persistence;
using Notification.Application.Abstractions;
using Notification.Domain.Entities;
using Notification.Infrastructure;
using Notification.Infrastructure.Delivery;
using Notification.Persistence;
using Microsoft.AspNetCore.SignalR;

namespace NotificationIntegrationTests;

// These tests verify EF metadata and DI without opening a database connection.
public sealed class InfrastructureTests
{
    [Fact]
    public void PersistenceModelHasExplicitKeysAndNoShadowForeignKeys()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IDateTimeManager>(new FixedDateTimeManager(DateTimeOffset.UtcNow));
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
        Assert.Equal(new[] { "Id" }, notification.FindPrimaryKey()!.Properties.Select(property => property.Name));
        Assert.Contains(notification.GetIndexes(), index => index.IsUnique &&
            index.Properties.Select(property => property.Name).SequenceEqual(new[] { "UserId", "Kind", "BusinessId" }));
        Assert.Equal("jsonb", notification.FindProperty(nameof(UserNotification.Content))!.GetColumnType());
        Assert.Equal(3, model.GetEntityTypes().Count());
        Assert.NotNull(model.FindEntityType(typeof(NotificationWork)));
        Assert.NotNull(model.FindEntityType(typeof(UserNotificationPreference)));
        Assert.DoesNotContain(model.GetEntityTypes(), entity =>
            entity.ClrType.Namespace?.StartsWith("Notification.Persistence", StringComparison.Ordinal) == true);
        Assert.All(model.GetEntityTypes().SelectMany(entity => entity.GetForeignKeys()).SelectMany(key => key.Properties),
            property => Assert.False(property.IsShadowProperty()));
        Assert.Contains("CREATE TABLE", context.Database.GenerateCreateScript());
        var historyScript = context.GetService<IHistoryRepository>().GetCreateScript();
        Assert.Contains("\"Notification\".\"_Notification_MigrationsHistory\"", historyScript);
        Assert.IsAssignableFrom<BaseDbContext>(context);
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IDbInitializer>());
        var storeTypes = new[]
        {
            scope.ServiceProvider.GetRequiredService<INotificationStore>().GetType(),
            scope.ServiceProvider.GetRequiredService<INotificationPreferenceStore>().GetType(),
            scope.ServiceProvider.GetRequiredService<IFanoutStore>().GetType(),
            scope.ServiceProvider.GetRequiredService<INotificationWorkStore>().GetType()
        };
        Assert.Equal(4, storeTypes.Distinct().Count());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IReadRepository<INotificationEntity>>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IWriteRepository<INotificationEntity>>());
        var repository = scope.ServiceProvider.GetRequiredService<IReadWriteRepository<INotificationEntity>>();
        var entity = UserNotification.Create(Guid.NewGuid(), Guid.NewGuid(), new("tests", Guid.NewGuid()),
            new(NotificationKind.CommentReply, Guid.NewGuid(), Guid.NewGuid(),
                new(NotificationTargetType.Comment, Guid.NewGuid()), "comment.reply", 1,
                new Dictionary<string, string>(), DateTimeOffset.UtcNow), DateTimeOffset.UtcNow);
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

    [Fact]
    public void InfrastructureReplacesTheDefaultSignalRUserIdProvider()
    {
        var services = new ServiceCollection();
        services.AddNotificationInfrastructure();
        using var provider = services.BuildServiceProvider();

        Assert.IsType<NotificationUserIdProvider>(provider.GetRequiredService<IUserIdProvider>());
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class FixedDateTimeManager(DateTimeOffset now) : IDateTimeManager
    {
        public DateTimeOffset UtcNow() => now;
    }
}
