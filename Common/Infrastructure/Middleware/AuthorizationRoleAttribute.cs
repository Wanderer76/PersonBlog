using Microsoft.AspNetCore.Mvc.Filters;

namespace Infrastructure.Middleware
{
    public class AuthorizationRoleAttribute : Attribute, IAuthorizationFilter
    {
        private readonly List<Guid> _roles;

        public AuthorizationRoleAttribute(params Guid[] roles)
        {
            _roles = [.. roles];
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            throw new NotImplementedException();
        }
    }
}
