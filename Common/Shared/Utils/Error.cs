namespace Shared.Utils
{
    public class Error
    {
        public string Message { get; }
        public string Code { get; }

        public Error(string message)
            : this("400", message)
        {
        }

        public Error(string code, string message)
        {
            Code = code;
            Message = message;
        }

        public ErrorList ToErrorList() => new([this]);
    }
}
