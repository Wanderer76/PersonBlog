using Microsoft.AspNetCore.Http;

namespace Shared.Services
{
    public static class HttpContextExtensions
    {
        public static Guid GetUserFromContext(this HttpContext context)
        {
            return Guid.Parse(context.User.Claims.First(x => x.Type == AppClaimTypes.UserId).Value);
        }
    }
}
