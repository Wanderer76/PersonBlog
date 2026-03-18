namespace Shared.Utils;

public sealed class AsyncProgress<T>(Func<T, Task> progressCallback)
{
    private readonly Func<T, Task> _callback = progressCallback;

    public Task InvokeAsync(T value)
    {
        return _callback(value);
    }

    public static implicit operator Func<T, Task>(AsyncProgress<T> progress) => progress._callback;

    public static implicit operator AsyncProgress<T>(Func<T, Task> func) => new(func);
}
