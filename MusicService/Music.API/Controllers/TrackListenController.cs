using Authentication.Contract.Constants;
using Infrastructure.Middleware;
using Infrastructure.Models;
using Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Music.Contract.Events;
using Music.Domain.Entities;
using Shared.Persistence;
using Shared.Services;

namespace Music.API.Controllers
{
    public class TrackListenController : BaseController
    {
        private readonly IReadRepository<IMusicEntity> _read;
        private readonly IWriteRepository<IMusicEntity> _writeRepository;
        private readonly IFileStorageFactory _fileStorageFactory;
        private readonly ICacheService _cacheService;
        private readonly ICurrentUserService _userUserService;
        public TrackListenController(ILogger<BaseController> logger, IReadRepository<IMusicEntity> read, IFileStorageFactory fileStorageFactory, ICacheService cacheService, ICurrentUserService urrentUserService, IWriteRepository<IMusicEntity> writeRepository) : base(logger)
        {
            _read = read;
            _fileStorageFactory = fileStorageFactory;
            _cacheService = cacheService;
            _userUserService = urrentUserService;
            _writeRepository = writeRepository;
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

        [HttpPost("track/{trackId}/listen")]
        [AuthFilter(Roles.User)]
        public async Task<IActionResult> AddTrackToUserListenHistory(Guid trackId)
        {
            var user = await _userUserService.GetCurrentUserAsync();
            var listenEvent = new ListenHistoryEvent(GuidService.GetNewGuid(), user.UserId, trackId, DateTimeService.Now());
            _writeRepository.Add(MusicEvents.Create(listenEvent));
            await _writeRepository.SaveChangesAsync();
            return Ok();
        }
    }

    file class TrackUrlCacheKey(Guid Id) : ICacheKey
    {
        private const string Key = nameof(TrackUrlCacheKey);
        public string GetKey() => $"{Key}:{Id}";
    }
}
