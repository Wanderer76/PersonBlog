using Blog.Service.Models;
using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;
using System.Web;

namespace VideoView.Application.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RecommendationController : BaseController
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public RecommendationController(ILogger<RecommendationController> logger, IHttpClientFactory httpClientFactory) : base(logger)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet("/recommendations")]
        public async Task<IActionResult> GetRecommendedPosts(int page, int limit, Guid? currentPostId)
        {
            using var client = _httpClientFactory.CreateClient("Recommendation");
            var query = HttpUtility.ParseQueryString(string.Empty);
            if (currentPostId.HasValue)
            {
                query["currentPostId"] = HttpUtility.UrlEncode(currentPostId.Value.ToString());
            }
            query["page"] = HttpUtility.UrlEncode(page.ToString());
            query["pageSize"] = HttpUtility.UrlEncode(limit.ToString());

            var result = await client.GetFromJsonAsync<IEnumerable<VideoCardModel>>($"Content/recommendations?{query}");
            return Ok(result);
        }
    }
}
