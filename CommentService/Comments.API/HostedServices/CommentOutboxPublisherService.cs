using Comments.Service.Implementation;

namespace Comments.API.HostedServices;

public sealed class CommentOutboxPublisherService(IServiceProvider services) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<CommentOutboxPublisher>()
                .PublishPendingAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
