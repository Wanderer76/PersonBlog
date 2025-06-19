using Blog.Service.Models;
using Blog.Service.Models.Post;
using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;
using Shared.Utils;
using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Text.Json;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace VideoView.Application.Controllers
{
    public class SearchController : BaseController
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public SearchController(ILogger<BaseController> logger, IHttpClientFactory httpClientFactory) : base(logger)
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
