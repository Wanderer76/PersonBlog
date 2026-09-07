namespace Notification.Infrastructure.BackgroundJobs;

public sealed class NotificationWorkerOptions
{
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(2);
    public TimeSpan LeaseDuration { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(30);
    public int MaxAttempts { get; set; } = 10;
    public int FanoutPageSize { get; set; } = 500;
}
