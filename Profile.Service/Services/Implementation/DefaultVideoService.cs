using Blog.Domain.Entities;
using Blog.Service.Models.File;
using FileStorage.Service.Models;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Persistence;
using Shared.Services;

namespace Blog.Service.Services.Implementation
{
    internal class DefaultVideoService : IVideoService
    {
        private readonly IReadWriteRepository<IProfileEntity> _context;
        private readonly ICacheService _cacheService;
        public DefaultVideoService(IReadWriteRepository<IProfileEntity> context, ICacheService cacheService)
        {
            _context = context;
            _cacheService = cacheService;
        }

        public async Task<VideoMetadata> GetOrCreateVideoMetadata(UploadVideoChunkModel uploadVideoChunk)
        {
            var cacheKey = $"{nameof(VideoMetadata)}:{uploadVideoChunk.PostId}";

            var cacheResult = await _cacheService.GetCachedDataAsync<VideoMetadata>(cacheKey);

            if (cacheResult != null)
            {
                return cacheResult;
            }

            var metadata = await _context.Get<VideoMetadata>()
                .FirstOrDefaultAsync(x => x.PostId == uploadVideoChunk.PostId);

            var post = await _context.Get<Post>()
                .FirstAsync(x => x.Id == uploadVideoChunk.PostId);

            _context.Attach(post);
            post.Type = PostType.Video;

            if (metadata == null)
            {
                try
                {
                    metadata = new VideoMetadata
                    {
                        Id = GuidService.GetNewGuid(),
                        FileExtension = uploadVideoChunk.FileExtension,
                        CreatedAt = DateTimeOffset.UtcNow,
                        ContentType = uploadVideoChunk.ContentType,
                        PostId = uploadVideoChunk.PostId,
                        IsProcessed = true,
                        Name = uploadVideoChunk.FileName,
                        Resolution = VideoResolution.Original,
                        Duration = uploadVideoChunk.Duration,
                        ObjectName = string.Empty,
                        Length = uploadVideoChunk.TotalSize
                    };
                    await _cacheService.SetCachedDataAsync(cacheKey, metadata, TimeSpan.FromMinutes(10));
                    _context.Add(metadata);
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    await _cacheService.RemoveCachedDataAsync(cacheKey);
                }
            }
            else
            {
                await _cacheService.SetCachedDataAsync(cacheKey, metadata, TimeSpan.FromMinutes(10));
            }

            return metadata!;
        }
    }
}
