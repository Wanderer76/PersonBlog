using Infrastructure.Models;

namespace AuthenticationApplication.Controllers
{
    public class TokenController : BaseController
    {
        public TokenController(ILogger<BaseController> logger) : base(logger)
        {
        }
    }
}
