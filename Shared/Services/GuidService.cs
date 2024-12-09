using Shared.Persistence;

namespace Shared.Services
{
    public static class GuidService
    {
        public static Guid GetNewGuid(this IReadRepository _)
        {
            return Guid.NewGuid();
        }
    }
}
