using Blog.Service.Service;
using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;

namespace VideoView.Application.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RecommendationController : BaseController
    {
        private readonly IRecommendationService _recommendationService;
        private readonly IDistributedCache _cache;
        public RecommendationController(ILogger<RecommendationController> logger, IRecommendationService recommendationService, IDistributedCache cache) : base(logger)
        {
            _recommendationService = recommendationService;
            _cache = cache;
        }

        [HttpGet("/recommendations")]
        public async Task<IActionResult> GetRecommendedPosts(int page, int limit, Guid? currentPostId)
        {
            return Ok(await _recommendationService.GetRecommendations(page, limit, currentPostId));
        }
    }
}
