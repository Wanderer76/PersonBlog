namespace Shared.Utils;

public class Error
{
    public string Message { get; }
    public string Key { get; }

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
