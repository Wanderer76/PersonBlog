namespace Blog.Service.Exceptions
{
    [Serializable]
    internal class BlogAlreadyExistsException : Exception
    {
        public BlogAlreadyExistsException()
        {
        }

        public BlogAlreadyExistsException(string? message) : base(message)
        {
        }

        public BlogAlreadyExistsException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}