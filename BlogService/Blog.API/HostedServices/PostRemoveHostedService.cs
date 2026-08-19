
using Blog.Service.Services.Implementation;
using Shared.Services;

namespace Blog.API.HostedServices
{
    public sealed class PostRemoveHostedService(IServiceProvider serviceProvider) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = serviceProvider.CreateScope())
                {
                    var cleanupService = scope.ServiceProvider.GetRequiredService<PostFileCleanupService>();
                    await cleanupService.CleanupExpiredAsync(
                        DateTimeService.Now().AddDays(-2),
                        stoppingToken);
                }

                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
        }
    }
}
