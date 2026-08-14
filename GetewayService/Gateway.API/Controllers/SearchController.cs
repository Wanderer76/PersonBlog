using Blog.Contracts.Models.Post;
using Gateway.API.Api;
using Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Encodings.Web;

namespace Gateway.API.Controllers;

public sealed class SearchController : BaseApiController
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly BlogFeedApiClient _blogClient;

    public SearchController(
        ILogger<BaseApiController> logger,
        IHttpClientFactory httpClientFactory,
        BlogFeedApiClient blogClient) : base(logger)
    {
        _httpClientFactory = httpClientFactory;
        _blogClient = blogClient;
    }

    [HttpGet("searchByTitle")]
    public async Task<IActionResult> SearchPostsByTitle(
        string title,
        CancellationToken cancellationToken)
    {
        using var searchClient = _httpClientFactory.CreateClient("Search");
        var searchString = UrlEncoder.Default.Encode(title).Trim();
        var searchResult = await searchClient.GetFromJsonAsync<IEnumerable<PostModel>>(
            $"PostSearch?title={searchString}",
            cancellationToken) ?? [];
        var postIds = searchResult.Select(post => post.Id).ToArray();

        return Ok(await _blogClient.GetPostCardsAsync(postIds, cancellationToken));
    }
}
