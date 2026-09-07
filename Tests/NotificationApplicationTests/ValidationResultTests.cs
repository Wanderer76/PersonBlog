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
        public Task<Result<IReadOnlyList<NotificationPreference>>> GetAsync(Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<IReadOnlyList<NotificationPreference>>.Success([]));

        public Task<Result> UpdateAsync(Guid userId, IReadOnlyList<NotificationPreference> preferences,
            DateTimeOffset updatedAt, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success());
    }

    private sealed class NotificationStoreStub : INotificationStore
    {
        public bool WasCalled { get; private set; }

        public Task<Result<CreationResult>> CompleteCreationAsync(InboxCheckpoint checkpoint,
            NotificationDraft? draft, CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            return Task.FromResult(Result<CreationResult>.Success(new CreationResult(CreationStatus.Created, draft?.Id)));
        }

        public Task<Result<NotificationPage>> ListAsync(Guid userId, NotificationCursor? cursor, int limit,
            bool unreadOnly, DateTimeOffset snapshotAt, CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            return Task.FromResult(Result<NotificationPage>.Success(new NotificationPage([], null, snapshotAt)));
        }

        public Task<Result<long>> CountUnreadAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<long>.Success(0L));

        public Task<Result<bool>> MarkReadAsync(Guid userId, Guid notificationId, DateTimeOffset readAt,
            CancellationToken cancellationToken = default) => Task.FromResult(Result<bool>.Success(false));

        public Task<Result<int>> MarkAllReadAsync(Guid userId, DateTimeOffset snapshotAt, DateTimeOffset readAt,
            CancellationToken cancellationToken = default) => Task.FromResult(Result<int>.Success(0));
    }
}
