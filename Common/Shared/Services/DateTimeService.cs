namespace Shared.Services;

public interface IDateTimeManager
{
    DateTimeOffset UtcNow();
}

public class DateTimeService : IDateTimeManager
{
    public static DateTimeOffset Now() => DateTimeOffset.UtcNow;
    public DateTimeOffset UtcNow() => DateTimeOffset.UtcNow;
}
