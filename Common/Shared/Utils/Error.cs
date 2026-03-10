namespace Shared.Utils;

public sealed class Error
{
    public string Key { get; }
    public string Message { get; }

    public Error(string message)
        : this("", message)
    {
    }

    public Error(string key, string message)
    {
        Key = key;
        Message = message;
    }

    public ErrorList ToErrorList() => new([this]);
}
