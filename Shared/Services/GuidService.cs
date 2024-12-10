using Shared.Persistence;

namespace Shared.Services
{
    public static class GuidService
    {
        public static Guid GetNewGuid()
        {
            return Guid.NewGuid();
        }
    }
}
