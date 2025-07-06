using Blog.Domain.Services.Models;
using FileStorage.Service.Service;
using Infrastructure.Models;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
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
    [Authorize]
    public async Task<IActionResult> GetViewHistory()
    {
        HttpContext.TryGetUserFromContext(out var userId);

        var result = await _cache.GetOrAddDataAsync(new ViewCacheModel(userId.Value), async () =>
        {
            var historyItems = userId.HasValue ?
           await _httpClientFactory.CreateClient("Reacting").GetFromJsonAsync<IReadOnlyList<HistoryViewItem>>($"ViewHistory/list/{userId.Value}")
           : [];

            var postPreviews = historyItems!
               .DistinctBy(x => x.PostId)
               .Select(x => _httpClientFactory.GetPostDetailViewAsync(x.PostId))
               .ToList();

            var previews = (await Task.WhenAll(postPreviews))
                .Where(x => !x.IsFailure)
                .Select(x => x.Value)
                .ToDictionary(x => x.Id);

            return historyItems!
                        .Where(x => previews.ContainsKey(x.PostId))
                        .Select(x => new UserHistoryViewItem(
                            x.Id,
                            x.PostId,
                            x.LastWatched,
                            x.WatchedTime,
                            previews[x.PostId]
                        ))
                        .GroupBy(x => x.LastWatched.Date)
                        .OrderByDescending(x => x.Key)
                        .ToDictionary(x => x.Key, x => x.Select(a => a).ToList());
        });

        return Ok(result);
    }
}

internal record UserHistoryViewItem(Guid Id, Guid PostId, DateTime LastWatched, double WatchedTime, PostDetailViewModel PostDetail);

public sealed class ViewCacheModel(Guid userId) : ICacheKey
{
    private const string Key = nameof(ViewCacheModel);
    public Guid UserId { get; } = userId;
    public string GetKey() => $"{Key}:{UserId}";
}
