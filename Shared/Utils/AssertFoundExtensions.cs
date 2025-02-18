namespace Shared.Utils
{
    public static class AssertFoundExtensions
    {

        public static void AssertFound<T>(this T onFound, string? message = null)
        {
            if (onFound == null)
            {
                throw new ArgumentNullException(message ?? "Ресурс не найден");
            }
        }
    }
}
