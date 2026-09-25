using Conference.Service.Implementation;

namespace Conference.API.HostedServices;

public sealed class ConferenceOutboxPublisherService(IServiceProvider services) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<ConferenceOutboxPublisher>()
                .PublishPendingAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
