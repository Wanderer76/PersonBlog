namespace Shared.Services;
public interface IHave<out T>
{
    T Value { get; }
}

public static class IHaveExtensions
{
    public static T Get<T>(this IHave<T> self) => self.Value;
}