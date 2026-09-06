namespace Shared.Services;

public interface IDateTimeManager
{
    public static DateTimeOffset Now() => DateTimeService.Now();
    DateTimeOffset UtcNow() => DateTimeService.Now();
}

public static class DateTimeService
{
    public static DateTimeOffset Now() => DateTimeOffset.UtcNow;
}