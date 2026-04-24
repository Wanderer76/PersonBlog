using Microsoft.Extensions.Hosting;

namespace MessageBus.Internal;

internal class DefaultHostedService : IHostedService
{
    private readonly RabbitMqMessageBus _messageBus;

    public DefaultHostedService(RabbitMqMessageBus messageBus)
    {
        _messageBus = messageBus;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _messageBus.InitializeSubscriptionAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _messageBus.DisposeAsync(); // или отдельный метод Stop()
    }
}
