using Blog.Contracts;
using Infrastructure.Models;

namespace Gateway.API.Controllers.Blog;

public class PostController : BaseApiController
{
    private readonly PostApiClient postApiClient;
    public PostController(ILogger<BaseApiController> logger) : base(logger)
    {
    }
}
