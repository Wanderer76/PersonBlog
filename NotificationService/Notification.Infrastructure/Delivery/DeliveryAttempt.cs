using System.IO;

namespace Notification.Infrastructure.Delivery;

internal static class DeliveryAttempt
{
    public static async Task<Result> Run(Guid jobId, string destination, Func<Task<Result>> send,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (jobId == Guid.Empty || string.IsNullOrWhiteSpace(destination))
            return Result.Failure("Delivery.InvalidDestination", "A job id and destination are required.");
        try
        {
            return await send();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Result.Failure("Delivery.Timeout", "Delivery timed out.");
        }
        catch (HttpRequestException)
        {
            return Result.Failure("Delivery.Unavailable", "The delivery provider is unavailable.");
        }
        catch (IOException)
        {
            return Result.Failure("Delivery.Unavailable", "The delivery connection failed.");
        }
    }
}
