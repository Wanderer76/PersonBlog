using Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Middleware
{
    public class AuthFilterAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private readonly List<Guid> _roles;

        public AuthFilterAttribute(params string[] roles)
        {
            if (roles == null)
                _roles = [];
            else
                _roles = [.. roles.Select(Guid.Parse)];
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            if (_roles.Count == 0)
            {
                return;
            }
            var currentUser = await context.HttpContext.RequestServices.GetRequiredService<ICurrentUserService>().GetCurrentUserAsync();
            if (currentUser.IsAnonymous)
            {
                context.Result = new ForbidResult();
                return;
            }
            var roles = currentUser.Roles;
            if (!roles.Intersect(_roles).Any())
            {
                context.Result = new ForbidResult();
                return;
            }
        }

    }
}
