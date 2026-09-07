using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notification.Application.Abstractions;
using Notification.Application.Fanout;
using Notification.Application.Notifications;
using Notification.Application.Preferences;
using Notification.Domain.Entities;
using Shared.Services;

namespace Notification.Infrastructure.BackgroundJobs;

/// <summary>One bounded claim of each kind per invocation; durable state owns retries and deduplication.</summary>
public sealed class NotificationWorkProcessor(INotificationWorkStore workStore,
    CreateNotification create, StartCampaign start, ProcessRecipientBatch fanout,
    INotificationPreferenceStore preferences, IEnumerable<INotificationDelivery> deliveries,
    IDateTimeManager dateTimeManager, IOptions<NotificationWorkerOptions> options,
    ILogger<NotificationWorkProcessor> logger)
{
    public async Task ProcessOnceAsync(CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        var inbox = await workStore.ClaimInboxAsync(settings.LeaseDuration, cancellationToken);
        if (inbox.IsFailure) Log(inbox.Errors[0].Key);
        else if (inbox.Value is { } input)
            await Execute(NotificationWorkKind.Inbox, input.Id, input.Checkpoint.LeaseToken, async () =>
            {
                var message = input.Message;
                if (message.PublicationId is { } publicationId)
                {
                    var outcome = await start.ExecuteAsync(new StartCampaignCommand(input.Checkpoint, publicationId,
                        message.BlogId!.Value, message.IsPublic, message.AudienceCutoff!.Value, message.Content), cancellationToken);
                    return outcome.IsSuccess ? Result.Success() : Result.Failure(outcome.Errors);
                }
                var result = await create.ExecuteAsync(new CreateNotificationCommand(input.Checkpoint,
                    message.RecipientUserId!.Value, message.Content), dateTimeManager.UtcNow(), cancellationToken);
                return result.IsSuccess ? Result.Success() : Result.Failure(result.Errors);
            }, cancellationToken);

        var campaign = await workStore.ClaimCampaignAsync(settings.LeaseDuration, cancellationToken);
        if (campaign.IsFailure) Log(campaign.Errors[0].Key);
        else if (campaign.Value is { } batch)
            await Execute(NotificationWorkKind.Fanout, batch.Campaign.Id, batch.LeaseToken,
                () => fanout.ExecuteAsync(batch, dateTimeManager.UtcNow(), settings.FanoutPageSize, cancellationToken),
                cancellationToken);

        var delivery = await workStore.ClaimDeliveryAsync(settings.LeaseDuration, cancellationToken);
        if (delivery.IsFailure) Log(delivery.Errors[0].Key);
        else if (delivery.Value is { } job)
            await Execute(NotificationWorkKind.Delivery, job.Id, job.LeaseToken, async () =>
            {
                if (job.Notification.Content.ExpiresAt <= dateTimeManager.UtcNow())
                    return await workStore.CompleteDeliveryAsync(job.Id, job.LeaseToken, cancellationToken);
                var stored = await preferences.GetAsync(job.Notification.UserId, cancellationToken);
                if (stored.IsFailure) return Result.Failure(stored.Errors);
                if (!NotificationPreferenceResolver.IsEnabled(job.Notification.Content.Kind, job.Channel, stored.Value))
                    return await workStore.CompleteDeliveryAsync(job.Id, job.LeaseToken, cancellationToken);
                var adapter = deliveries.SingleOrDefault(x => x.Channel == job.Channel);
                if (adapter is null) return Result.Failure("Delivery.UnsupportedChannel", "No delivery adapter is configured.");
                var result = await adapter.DeliverAsync(job.Id, job.DestinationKey, job.Notification, cancellationToken);
                return result.IsFailure ? result : await workStore.CompleteDeliveryAsync(job.Id, job.LeaseToken, cancellationToken);
            }, cancellationToken);
    }

    private async Task Execute(NotificationWorkKind kind, Guid id, Guid lease, Func<Task<Result>> action, CancellationToken ct)
    {
        Result outcome;
        try { outcome = await action(); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception exception)
        {
            // A hosted-service boundary quarantines poison jobs without terminating other work.
            logger.LogError(exception, "Notification job {Kind}/{Id} failed", kind, id);
            outcome = Result.Failure("Work.Unexpected", "The job could not be processed.");
        }
        if (outcome.IsSuccess) return;
        Log(outcome.Errors[0].Key);
        var released = await workStore.FailAsync(kind, id, lease, outcome.Errors[0].Key ?? "Work.Failed",
            options.Value.RetryDelay, options.Value.MaxAttempts, ct);
        if (released.IsFailure) Log(released.Errors[0].Key);
    }

    private void Log(string? key) => logger.LogWarning("Notification work returned {ErrorCode}", key);
}
