using Blog.Service.Models;
using Blog.Service.Models.Post;
using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Encodings.Web;

namespace Gateway.API.Controllers
{
    public class SearchController : BaseApiController
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public SearchController(ILogger<BaseApiController> logger, IHttpClientFactory httpClientFactory) : base(logger)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet("searchByTitle")]
        public async Task<IActionResult> SearchPostsByTitle(string title)
        {
            using var searchClient = _httpClientFactory.CreateClient("Search");
            var searchSting = UrlEncoder.Default.Encode(title).Trim();
            var searchResult = await searchClient.GetFromJsonAsync<IEnumerable<PostModel>>($"PostSearch?title={searchSting}");

            if (!searchResult.Any())
                return Ok(new List<VideoCardModel>());

            using var postClient = _httpClientFactory.CreateClient("Recommendation");
            var result = await postClient.PostAsJsonAsync($"Content/postListByIds", new { PostIds = searchResult.Select(x => x.Id).ToList() });

            return Ok(await result.Content.ReadAsStringAsync());
        }
    }
}
