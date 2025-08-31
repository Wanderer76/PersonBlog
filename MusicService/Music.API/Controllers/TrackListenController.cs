using Infrastructure.Models;
using Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Music.Domain.Entities;
using Shared.Persistence;

namespace Music.API.Controllers
{
    public class TrackListenController : BaseController
    {
        private readonly IReadRepository<IMusicEntity> _read;
        private readonly IFileStorageFactory _fileStorageFactory;
        public TrackListenController(ILogger<BaseController> logger, IReadRepository<IMusicEntity> read, IFileStorageFactory fileStorageFactory) : base(logger)
        {
            _read = read;
            _fileStorageFactory = fileStorageFactory;
        }

        [HttpGet("track/{trackId}")]
        public async Task<IActionResult> RedirectToPresignedUrl(Guid trackId)
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
            var presignedUrl = await fileStorage.GetFileUrlAsync(trackFile.Id, trackFile.ObjectName);
            return Ok(presignedUrl);
        }
    }
}
