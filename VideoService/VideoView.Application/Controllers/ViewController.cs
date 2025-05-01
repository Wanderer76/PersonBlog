using FileStorage.Service.Service;
using Infrastructure.Models;
using Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Shared.Services;
using VideoView.Application.Api;
using ViewReacting.Domain.Models;

namespace VideoView.Application.Controllers;

public class ViewController : BaseController
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ICacheService _cache;
    private readonly IFileStorage _storage;

    public ViewController(ILogger<ViewController> logger, IHttpClientFactory httpClientFactory, ICacheService cache) : base(logger)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
    }


    [HttpGet("history")]
    public async Task<IActionResult> GetViewHistory()
    {
        HttpContext.TryGetUserFromContext(out var userId);

        var result = userId.HasValue ?
            await _httpClientFactory.CreateClient("Reacting").GetFromJsonAsync<IReadOnlyList<HistoryViewItem>>($"ViewHistory/list/{userId.Value}")
            : [];

        var postPreviews = result!
           .DistinctBy(x => x.PostId)
           .Select(x => _httpClientFactory.GetPostDetailViewAsync(x.PostId))
           .ToList();

        var previews = (await Task.WhenAll(postPreviews))
            .Where(x => !x.IsFailure)
            .Select(x => x.Value)
            .ToDictionary(x => x.Id);

        return Ok(result!
            .Select(x => new
            {
                x.Id,
                x.PostId,
                x.LastWatched,
                x.WatchedTime,
                PostDetail = previews[x.PostId]
            })
            .GroupBy(x => x.LastWatched.Date)
            .OrderByDescending(x=>x.Key)
            .ToDictionary(x => x.Key, x => x.Select(a => a).ToList()));
    }
}