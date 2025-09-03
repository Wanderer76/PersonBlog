using Infrastructure.Models;
using Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Music.Domain.Entities;
using Shared.Persistence;
using Shared.Services;

namespace Music.API.Controllers
{
    public class TrackListenController : BaseController
    {
        private readonly IReadRepository<IMusicEntity> _read;
        private readonly IFileStorageFactory _fileStorageFactory;
        private readonly ICacheService _cacheService;
        public TrackListenController(ILogger<BaseController> logger, IReadRepository<IMusicEntity> read, IFileStorageFactory fileStorageFactory, ICacheService cacheService) : base(logger)
        {
            _read = read;
            _fileStorageFactory = fileStorageFactory;
            _cacheService = cacheService;
        }

        [HttpGet("track/{trackId}")]
        public async Task<IActionResult> RedirectToPresignedUrl(Guid trackId)
        {
            var presignedUrl = await _cacheService.GetOrAddDataAsync(new TrackUrlCacheKey(trackId), async () =>
            {
                var trackFile = await _read.Get<TrackMetadata>()
                .Where(x => x.TrackId == trackId)
                .Select(x => new
                {
                    x.Id,
                    x.ObjectName
                })
                .FirstAsync();
                using var fileStorage = _fileStorageFactory.CreateFileStorage();
                return await fileStorage.GetFileUrlAsync(trackFile.Id, trackFile.ObjectName);
            });

            return Ok(presignedUrl);
        }
    }

    file class TrackUrlCacheKey(Guid Id) : ICacheKey
    {
        private const string Key = nameof(TrackUrlCacheKey);
        public string GetKey() => $"{Key}:{Id}";
    }
}
