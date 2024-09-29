namespace Shared.Utils
{
    public static class AssertFoundExtensions
    {

        public static void AssertFound(this object onFound, string message = null)
        {
            if (onFound == null)
            {
                throw new ArgumentNullException(message ?? "Ресурс не найден");
            }
        }
    }
}
