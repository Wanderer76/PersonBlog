using System.Text.Json.Serialization;

namespace Shared.Utils;

public sealed class Error : IResultError
{
    public string Key { get; }
    public string Message { get; }

    public Error()
    {
        
    }

    public Error(string message)
        : this("", message)
    {
    }

    [JsonConstructor]
    public Error(string key, string message)
    {
        Key = key;
        Message = message;
    }

    public ErrorList ToErrorList() => new([this]);
}

public static class ErrorConstants
{
    public const string NotFound = "NotFound";
    public const string Forbidden = "Forbidden";
}