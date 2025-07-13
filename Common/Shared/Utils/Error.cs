namespace Shared.Utils
{
    public class Error
    {
        public string Message { get; }
        public string key { get; }

        public Error(string message)
            : this("", message)
        {
        }

        public Error(string key, string message)
        {
            this.key = key;
            Message = message;
        }

        public ErrorList ToErrorList() => new([this]);
    }
}
