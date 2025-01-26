using Microsoft.AspNetCore.Http;

namespace Shared.Services
{
    public static class HttpContextExtensions
    {
        public static Guid GetUserFromContext(this HttpContext context)
        {
            return Guid.Parse(context.User.Claims.First(x => x.Type == AppClaimTypes.UserId).Value);
        }
        public static Guid? GetOptionalUserFromContext(this HttpContext context)
        {
            var user = context.User.Claims.FirstOrDefault(x => x.Type == AppClaimTypes.UserId)?.Value;
            return user == null ? null : Guid.Parse(user!);
        }
        //public static Guid? GetProfileFromContext(this HttpContext context)
        //{
        //    var claims = context.User.Claims.FirstOrDefault(x => x.Type == AppClaimTypes.ProfileId)?.Value;
        //    if (claims == null) return null;
        //    Guid.TryParse(claims, out var result);
        //    return result;
        //}
    }
}
