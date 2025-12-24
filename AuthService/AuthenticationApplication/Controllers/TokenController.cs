using Infrastructure.Models;

namespace AuthenticationApplication.Controllers
{
    public class TokenController : BaseApiController
    {
        public TokenController(ILogger<BaseApiController> logger) : base(logger)
        {
        }
    }
}
