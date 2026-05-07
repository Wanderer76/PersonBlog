using Microsoft.Extensions.Hosting;

namespace MessageBus.Internal;

internal sealed class DefaultHostedService(IMessageSubscriber messageBus) : IHostedService, IAsyncDisposable
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await messageBus.InitializeSubscriptionAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        return messageBus.DisposeAsync();
    }
}
