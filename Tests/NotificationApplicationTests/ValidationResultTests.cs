using Infrastructure.Services;
using Notification.Application.Abstractions;
using Notification.Application.Notifications;
using Notification.Application.Preferences;
using Shared.Models;

namespace NotificationApplicationTests;

public sealed class ValidationResultTests
{
    [Fact]
    public async Task CreateReturnsFailureForMissingCommandWithoutCallingStore()
    {
        var store = new NotificationStoreStub();
        var handler = new CreateNotification(store, new PreferenceStoreStub());

        var result = await handler.ExecuteAsync(null, DateTimeOffset.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal("command", Assert.Single(result.Errors).Key);
        Assert.False(store.WasCalled);
    }

    [Fact]
    public async Task ListReturnsFailureForInvalidLimitWithoutCallingStore()
    {
        var store = new NotificationStoreStub();
        var handler = new GetNotifications(
            store, new CurrentUserServiceStub(Guid.NewGuid()));

        var result = await handler.ExecuteAsync(DateTimeOffset.UtcNow, limit: 0);

        Assert.True(result.IsFailure);
        Assert.Equal("limit", Assert.Single(result.Errors).Key);
        Assert.False(store.WasCalled);
    }

    [Fact]
    public async Task ListAcceptsSnapshotWithNonZeroOffset()
    {
        var store = new NotificationStoreStub();
        var handler = new GetNotifications(
            store, new CurrentUserServiceStub(Guid.NewGuid()));

        var snapshot = new DateTimeOffset(2026, 9, 6, 12, 0, 0, TimeSpan.FromHours(5));
        var result = await handler.ExecuteAsync(snapshot);

        Assert.True(result.IsSuccess);
        Assert.True(store.WasCalled);
    }

    private sealed class CurrentUserServiceStub(Guid userId) : ICurrentUserService
    {
        public Task<UserModel> GetCurrentUserAsync() => Task.FromResult(
            new UserModel(userId, "test-user", null, Guid.Empty, []));
    }

    private sealed class PreferenceStoreStub : INotificationPreferenceStore
    {
        public Task<IReadOnlyList<NotificationPreference>> GetAsync(Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<NotificationPreference>>([]);

        public Task UpdateAsync(Guid userId, IReadOnlyList<NotificationPreference> preferences,
            DateTimeOffset updatedAt, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class NotificationStoreStub : INotificationStore
    {
        public bool WasCalled { get; private set; }

        public Task<CreationResult> CompleteCreationAsync(InboxCheckpoint checkpoint,
            NotificationDraft? draft, CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            return Task.FromResult(new CreationResult(CreationStatus.Created, draft?.Id));
        }

        public Task<NotificationPage> ListAsync(Guid userId, NotificationCursor? cursor, int limit,
            bool unreadOnly, DateTimeOffset snapshotAt, CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            return Task.FromResult(new NotificationPage([], null, snapshotAt));
        }

        public Task<long> CountUnreadAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(0L);

        public Task<bool> MarkReadAsync(Guid userId, Guid notificationId, DateTimeOffset readAt,
            CancellationToken cancellationToken = default) => Task.FromResult(false);

        public Task<int> MarkAllReadAsync(Guid userId, DateTimeOffset snapshotAt, DateTimeOffset readAt,
            CancellationToken cancellationToken = default) => Task.FromResult(0);
    }
}
