namespace Profile.Service.Exceptions
{
    [Serializable]
    internal class ProfileNotFoundException : Exception
    {
        public ProfileNotFoundException()
        {
        }

        public ProfileNotFoundException(string? message) : base(message)
        {
        }

        public ProfileNotFoundException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}