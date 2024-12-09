namespace Profile.Service.Exceptions
{
    [Serializable]
    internal class BlogAlreaddyExistsException : Exception
    {
        public BlogAlreaddyExistsException()
        {
        }

        public BlogAlreaddyExistsException(string? message) : base(message)
        {
        }

        public BlogAlreaddyExistsException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}