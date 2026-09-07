using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Notification.Application.Abstractions;
using Notification.Application.Fanout;
using Notification.Application.Notifications;
using Notification.Application.Preferences;
using Notification.Domain.Entities;
using Notification.Infrastructure;
using Notification.Infrastructure.BackgroundJobs;
using Notification.Persistence;
using Npgsql;
using Shared.Services;

namespace NotificationIntegrationTests;

public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("NOTIFICATION_TEST_CONNECTION")))
            Skip = "Set NOTIFICATION_TEST_CONNECTION to a disposable PostgreSQL server.";
    }
}

public sealed class PostgresStoreTests : IAsyncLifetime
{
    private ServiceProvider provider = null!;
    private readonly TestClock clock = new();
    private readonly RecordingDelivery delivery = new();
    private readonly Recipients recipients = new();
    private INotificationStore Store => provider.GetRequiredService<INotificationStore>();
    private INotificationWorkStore Work => provider.GetRequiredService<INotificationWorkStore>();
    private IFanoutStore Fanout => provider.GetRequiredService<IFanoutStore>();
    private INotificationPreferenceStore Preferences => provider.GetRequiredService<INotificationPreferenceStore>();

    public async Task InitializeAsync()
    {
        var raw = Environment.GetEnvironmentVariable("NOTIFICATION_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(raw)) return;
        var connection = new NpgsqlConnectionStringBuilder(raw) { Database = "notification_test_" + Guid.NewGuid().ToString("N") };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:NotificationDbContext"] = connection.ConnectionString,
            ["AppUrls:NotificationProfile"] = "http://localhost/"
        }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<IDateTimeManager>(clock);
        services.AddNotificationInfrastructure();
        services.AddSingleton<IRecipientDirectory>(recipients);
        services.RemoveAll<INotificationDelivery>();
        services.AddSingleton<INotificationDelivery>(delivery);
        services.AddNotificationPersistence(configuration);
        services.AddNotificationWorkers();
        provider = services.BuildServiceProvider();
        await provider.GetRequiredService<NotificationDbContext>().Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (provider is null) return;
        await provider.GetRequiredService<NotificationDbContext>().Database.EnsureDeletedAsync();
        await provider.DisposeAsync();
    }

    [PostgresFact]
    public async Task DbContextPersistsDomainEntityAndItsReadStateDirectly()
    {
        var db = provider.GetRequiredService<NotificationDbContext>();
        var data = new Dictionary<string, string> { ["author"] = "Alice" };
        var content = Content() with { Data = data, ExpiresAt = clock.Now.AddDays(1) };
        var source = new NotificationSource("comments", Guid.NewGuid());
        var entity = UserNotification.Create(Guid.NewGuid(), Guid.NewGuid(), source, content, clock.Now);
        db.Notifications.Add(entity);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var loaded = await db.Notifications.SingleAsync(x => x.Id == entity.Id);
        Assert.Equal(source, loaded.Source);
        Assert.Equal(content.Kind, loaded.Kind);
        Assert.Equal(content.BusinessId, loaded.BusinessId);
        Assert.Equal(content.Target, loaded.Content.Target);
        Assert.Equal(content.ExpiresAt, loaded.Content.ExpiresAt);
        Assert.Equal("Alice", loaded.Content.Data["author"]);
        Assert.Throws<NotSupportedException>(() => ((IDictionary<string, string>)loaded.Content.Data)["author"] = "Changed");
        loaded.MarkRead(clock.Now.AddMinutes(1));
        await db.SaveChangesAsync();

        var page = (await Store.ListAsync(entity.UserId, null, 50, false, clock.Now)).Value;
        Assert.Equal(entity.Id, Assert.Single(page.Items).Id);
        Assert.Equal(clock.Now.AddMinutes(1), page.Items[0].ReadAt);
        Assert.Equal(0, (await Store.CountUnreadAsync(entity.UserId)).Value);
    }

    [PostgresFact]
    public async Task DuplicateBusinessKeyCompletesInboxWithoutCreatingAnotherDelivery()
    {
        var recipient = Guid.NewGuid();
        var content = Content();
        var first = await Input(recipient, content);
        var create = new CreateNotification(Store, Preferences);
        var created = await create.ExecuteAsync(new(first.Checkpoint, recipient, content), clock.Now);
        Assert.Equal(CreationStatus.Created, created.Value.Status);
        var second = await Input(recipient, content);
        var duplicate = await create.ExecuteAsync(new(second.Checkpoint, recipient, content), clock.Now);
        Assert.Equal(CreationStatus.Duplicate, duplicate.Value.Status);
        Assert.Equal(created.Value.NotificationId, duplicate.Value.NotificationId);
        Assert.Equal(1, (await Store.CountUnreadAsync(recipient)).Value);
        var delivery = (await Work.ClaimDeliveryAsync(TimeSpan.FromMinutes(1))).Value!;
        Assert.Equal(created.Value.NotificationId, delivery.Notification.Id);
        Assert.Null((await Work.ClaimDeliveryAsync(TimeSpan.FromMinutes(1))).Value);
        Assert.True((await Work.CompleteDeliveryAsync(delivery.Id, delivery.LeaseToken)).IsSuccess);
        Assert.Equal(1, (await Store.CountUnreadAsync(recipient)).Value);
        Assert.Null((await Work.ClaimInboxAsync(TimeSpan.FromMinutes(1))).Value);
    }

    [PostgresFact]
    public async Task ExpiredLeaseCannotWriteAndClaimCanBeRecovered()
    {
        var recipient = Guid.NewGuid();
        var content = Content();
        var original = await Input(recipient, content);
        clock.Now += TimeSpan.FromMinutes(2);
        var replacement = (await Work.ClaimInboxAsync(TimeSpan.FromMinutes(1))).Value!;
        Assert.NotEqual(original.Checkpoint.LeaseToken, replacement.Checkpoint.LeaseToken);
        var stale = await Store.CompleteCreationAsync(original.Checkpoint, Draft(recipient, content, original.Checkpoint.Source));
        Assert.Equal("Work.StaleLease", Assert.Single(stale.Errors).Key);
        Assert.Equal(0, (await Store.CountUnreadAsync(recipient)).Value);
        var result = await Store.CompleteCreationAsync(replacement.Checkpoint, Draft(recipient, content, replacement.Checkpoint.Source));
        Assert.True(result.IsSuccess);
    }

    [PostgresFact]
    public async Task KeysetPaginationAndReadOperationsRespectOwnershipAndSnapshot()
    {
        var recipient = Guid.NewGuid();
        var foreign = Guid.NewGuid();
        var ids = new List<Guid>();
        for (var i = 0; i < 3; i++)
        {
            var content = Content();
            var inbox = await Input(recipient, content);
            var result = await Store.CompleteCreationAsync(inbox.Checkpoint, Draft(recipient, content, inbox.Checkpoint.Source));
            ids.Add(result.Value.NotificationId!.Value);
        }
        var snapshot = clock.Now.ToOffset(TimeSpan.FromHours(5));
        var first = (await Store.ListAsync(recipient, null, 2, false, snapshot)).Value;
        var second = (await Store.ListAsync(recipient, first.NextCursor, 2, false, snapshot)).Value;
        Assert.Equal(3, first.Items.Concat(second.Items).Select(x => x.Id).Distinct().Count());
        Assert.Null(second.NextCursor);
        Assert.False((await Store.MarkReadAsync(foreign, ids[0], clock.Now)).Value);
        Assert.True((await Store.MarkReadAsync(recipient, ids[0], clock.Now)).Value);
        clock.Now += TimeSpan.FromSeconds(1);
        Assert.True((await Store.MarkReadAsync(recipient, ids[0], clock.Now)).Value);
        var newContent = Content();
        var newer = await Input(recipient, newContent);
        await Store.CompleteCreationAsync(newer.Checkpoint, Draft(recipient, newContent, newer.Checkpoint.Source));
        Assert.Equal(2, (await Store.MarkAllReadAsync(recipient, snapshot, clock.Now)).Value);
        Assert.Equal(1, (await Store.CountUnreadAsync(recipient)).Value);
        var all = (await Store.ListAsync(recipient, null, 100, false, clock.Now)).Value;
        Assert.Equal(snapshot.ToUniversalTime(), all.Items.Single(x => x.Id == ids[0]).ReadAt);
    }

    [PostgresFact]
    public async Task PreferenceUpsertsPreserveUnspecifiedKeys()
    {
        var user = Guid.NewGuid();
        await Preferences.UpdateAsync(user, [new(NotificationKind.CommentReply, DeliveryType.InApp, false),
            new(NotificationKind.PostPublished, DeliveryType.InApp, false)], clock.Now);
        await Preferences.UpdateAsync(user, [new(NotificationKind.CommentReply, DeliveryType.InApp, true)], clock.Now);
        var result = (await Preferences.GetAsync(user)).Value;
        Assert.Equal(2, result.Count);
        Assert.True(result.Single(x => x.Kind == NotificationKind.CommentReply).Enabled);
        Assert.False(result.Single(x => x.Kind == NotificationKind.PostPublished).Enabled);
    }

    [PostgresFact]
    public async Task FanoutRejectsStaleCursorAndRollsBackWholeBatchOnConstraintFailure()
    {
        var content = Content() with { Kind = NotificationKind.PostPublished };
        var source = new NotificationSource("blog", Guid.NewGuid());
        var ingress = new NotificationIngress(source, "publication", content, PublicationId: content.BusinessId,
            BlogId: Guid.NewGuid(), IsPublic: true, AudienceCutoff: clock.Now);
        Assert.True((await Work.EnqueueAsync(ingress)).IsSuccess);
        var inbox = (await Work.ClaimInboxAsync(TimeSpan.FromMinutes(1))).Value!;
        var campaign = new Campaign(Guid.NewGuid(), source, content.BusinessId, ingress.BlogId!.Value, clock.Now, content);
        Assert.True((await Fanout.CompleteStartAsync(inbox.Checkpoint, campaign)).IsSuccess);
        var claim = (await Work.ClaimCampaignAsync(TimeSpan.FromMinutes(1))).Value!;
        var recipient = Guid.NewGuid();
        var draft = Draft(recipient, content, source);
        // A database constraint failure must roll back the first insert and cursor.
        var conflicting = draft with { Id = Guid.NewGuid(), UserId = Guid.NewGuid(),
            Source = new NotificationSource(new string('x', 201), source.EventId) };
        Assert.True((await Fanout.CommitBatchAsync(claim, [draft, conflicting], "next", false)).IsFailure);
        Assert.Equal(0, (await Store.CountUnreadAsync(recipient)).Value);
        Assert.True((await Fanout.CommitBatchAsync(claim with { Cursor = "wrong" }, [draft], "next", false)).IsFailure);
        Assert.True((await Fanout.CommitBatchAsync(claim, [draft, draft], "next", false)).IsSuccess);
        Assert.Equal(1, (await Store.CountUnreadAsync(recipient)).Value);
        var next = (await Work.ClaimCampaignAsync(TimeSpan.FromMinutes(1))).Value!;
        Assert.Equal("next", next.Cursor);
        Assert.True((await Fanout.CommitBatchAsync(claim, [], null, true)).IsFailure);
        Assert.True((await Fanout.CommitBatchAsync(next, [], null, true)).IsSuccess);
        Assert.Null((await Work.ClaimCampaignAsync(TimeSpan.FromMinutes(1))).Value);
    }

    [PostgresFact]
    public async Task RetryIsDelayedAndEventuallyQuarantined()
    {
        var input = await Input(Guid.NewGuid(), Content());
        Assert.True((await Work.FailAsync(NotificationWorkKind.Inbox, input.Id, input.Checkpoint.LeaseToken,
            "Test.Failure", TimeSpan.FromSeconds(10), 2)).IsSuccess);
        Assert.Null((await Work.ClaimInboxAsync(TimeSpan.FromMinutes(1))).Value);
        clock.Now += TimeSpan.FromSeconds(11);
        var retry = (await Work.ClaimInboxAsync(TimeSpan.FromMinutes(1))).Value!;
        Assert.Equal(input.Id, retry.Id);
        Assert.True((await Work.FailAsync(NotificationWorkKind.Inbox, retry.Id, retry.Checkpoint.LeaseToken,
            "Test.Failure", TimeSpan.FromSeconds(10), 2)).IsSuccess);
        clock.Now += TimeSpan.FromHours(1);
        Assert.Null((await Work.ClaimInboxAsync(TimeSpan.FromMinutes(1))).Value);
    }

    [PostgresFact]
    public async Task ConcurrentCompletionCannotCreateDuplicateHistoryOrJobs()
    {
        var recipient = Guid.NewGuid();
        var content = Content();
        var input = await Input(recipient, content);
        var results = await Task.WhenAll(
            Store.CompleteCreationAsync(input.Checkpoint, Draft(recipient, content, input.Checkpoint.Source)),
            Store.CompleteCreationAsync(input.Checkpoint, Draft(recipient, content, input.Checkpoint.Source)));
        Assert.Single(results.Where(x => x.IsSuccess));
        Assert.Equal(1, (await Store.CountUnreadAsync(recipient)).Value);
        Assert.NotNull((await Work.ClaimDeliveryAsync(TimeSpan.FromMinutes(1))).Value);
        Assert.Null((await Work.ClaimDeliveryAsync(TimeSpan.FromMinutes(1))).Value);
    }

    [PostgresFact]
    public async Task DeliveryWorkerRechecksPreferencesAndExpiry()
    {
        var recipient = Guid.NewGuid();
        var content = Content();
        var input = await Input(recipient, content);
        await Store.CompleteCreationAsync(input.Checkpoint, Draft(recipient, content, input.Checkpoint.Source));
        await Preferences.UpdateAsync(recipient, [new(content.Kind, DeliveryType.InApp, false)], clock.Now);
        await provider.GetRequiredService<NotificationWorkProcessor>().ProcessOnceAsync();
        Assert.Null((await Work.ClaimDeliveryAsync(TimeSpan.FromMinutes(1))).Value);
        var expired = content with { BusinessId = Guid.NewGuid(), ExpiresAt = clock.Now };
        var expiredInput = await Input(recipient, expired);
        await Store.CompleteCreationAsync(expiredInput.Checkpoint, Draft(recipient, expired, expiredInput.Checkpoint.Source));
        await provider.GetRequiredService<NotificationWorkProcessor>().ProcessOnceAsync();
        Assert.Null((await Work.ClaimDeliveryAsync(TimeSpan.FromMinutes(1))).Value);
        Assert.Equal(2, (await Store.CountUnreadAsync(recipient)).Value);
        Assert.Equal(0, delivery.Count);
    }

    [PostgresFact]
    public async Task WorkerFanoutCommitsPagesAndSuppressesAuthorAndDisabledRecipients()
    {
        var author = Guid.NewGuid();
        var enabled = Guid.NewGuid();
        var disabled = Guid.NewGuid();
        var last = Guid.NewGuid();
        var content = Content() with { Kind = NotificationKind.PostPublished, ActorUserId = author };
        await Preferences.UpdateAsync(disabled, [new(NotificationKind.PostPublished, DeliveryType.InApp, false)], clock.Now);
        recipients.Pages.Enqueue(new([author, enabled, disabled, enabled], "second", true));
        recipients.Pages.Enqueue(new([enabled, last], null, false));
        Assert.True((await Work.EnqueueAsync(new(new("blog", Guid.NewGuid()), "publication", content,
            PublicationId: content.BusinessId, BlogId: Guid.NewGuid(), IsPublic: true, AudienceCutoff: clock.Now))).IsSuccess);
        var processor = provider.GetRequiredService<NotificationWorkProcessor>();
        await processor.ProcessOnceAsync();
        await processor.ProcessOnceAsync();
        Assert.Equal(0, (await Store.CountUnreadAsync(author)).Value);
        Assert.Equal(0, (await Store.CountUnreadAsync(disabled)).Value);
        Assert.Equal(1, (await Store.CountUnreadAsync(enabled)).Value);
        Assert.Equal(1, (await Store.CountUnreadAsync(last)).Value);
        Assert.Equal(2, delivery.Count);
        Assert.Equal(new string?[] { null, "second" }, recipients.Cursors);
        Assert.Null((await Work.ClaimCampaignAsync(TimeSpan.FromMinutes(1))).Value);
    }

    [PostgresFact]
    public async Task WorkerProcessesDurableIngressAndDeliveryWithoutMarkingRead()
    {
        var recipient = Guid.NewGuid();
        Assert.True((await Work.EnqueueAsync(new(new("comments", Guid.NewGuid()), "reply", Content(), recipient))).IsSuccess);
        await provider.GetRequiredService<NotificationWorkProcessor>().ProcessOnceAsync();
        Assert.Equal(1, (await Store.CountUnreadAsync(recipient)).Value);
        Assert.Null((await Work.ClaimDeliveryAsync(TimeSpan.FromMinutes(1))).Value);
        Assert.Null((await Work.ClaimInboxAsync(TimeSpan.FromMinutes(1))).Value);
        Assert.Equal(1, delivery.Count);
    }

    private async Task<ClaimedInbox> Input(Guid recipient, NotificationContent content)
    {
        var message = new NotificationIngress(new("tests", Guid.NewGuid()), "test-consumer", content, recipient);
        Assert.True((await Work.EnqueueAsync(message)).IsSuccess);
        var claim = await Work.ClaimInboxAsync(TimeSpan.FromMinutes(1));
        Assert.True(claim.IsSuccess);
        return Assert.IsType<ClaimedInbox>(claim.Value);
    }

    private NotificationDraft Draft(Guid user, NotificationContent content, NotificationSource source) =>
        new(Guid.NewGuid(), user, source, content, clock.Now, [DeliveryType.InApp]);
    private NotificationContent Content() => new(NotificationKind.CommentReply, Guid.NewGuid(), Guid.NewGuid(),
        new(NotificationTargetType.Comment, Guid.NewGuid()), "comment.reply", 1, new Dictionary<string, string>(), clock.Now);
    private sealed class TestClock : IDateTimeManager
    {
        public DateTimeOffset Now { get; set; } = new(2026, 9, 7, 10, 0, 0, TimeSpan.Zero);
        public DateTimeOffset UtcNow() => Now;
    }

    private sealed class RecordingDelivery : INotificationDelivery
    {
        public DeliveryType Channel => DeliveryType.InApp;
        public int Count { get; private set; }
        public Task<Result> DeliverAsync(Guid deliveryJobId, string destinationKey, NotificationItem notification,
            CancellationToken cancellationToken = default)
        {
            Count++;
            return Task.FromResult(Result.Success());
        }
    }

    private sealed class Recipients : IRecipientDirectory
    {
        public Queue<RecipientPage> Pages { get; } = new();
        public List<string?> Cursors { get; } = [];
        public Task<Result<RecipientPage>> GetRecipientsPageAsync(Guid blogId, string? cursor, int limit,
            DateTimeOffset cutoff, CancellationToken cancellationToken = default)
        {
            Cursors.Add(cursor);
            return Task.FromResult(Result<RecipientPage>.Success(Pages.Dequeue()));
        }
    }
}
