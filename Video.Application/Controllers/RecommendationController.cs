using Blog.Service.Service;
using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;

namespace VideoView.Application.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RecommendationController : BaseController
    {
        private readonly IRecommendationService _recommendationService;

        public RecommendationController(ILogger<BaseController> logger, IRecommendationService recommendationService) : base(logger)
        {
            _recommendationService = recommendationService;
        }

        [HttpGet("/recommendations")]
        public async Task<IActionResult> GetRecommendedPosts(int page, int limit, Guid? currentPostId)
        {
            return Ok(await _recommendationService.GetRecommendations(page, limit, currentPostId));
        }
    }
}
