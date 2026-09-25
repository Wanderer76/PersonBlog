using Shared.Services;

namespace Notification.Infrastructure.Services;

internal sealed class SystemDateTimeManager(TimeProvider timeProvider) : IDateTimeManager
{
    public DateTimeOffset UtcNow() => timeProvider.GetUtcNow();
}
