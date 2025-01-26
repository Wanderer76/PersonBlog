using FileStorage.Service.Models;
using Microsoft.EntityFrameworkCore;
using Profile.Domain.Entities;
using Profile.Service.Models.File;
using Shared.Persistence;
using Shared.Services;

namespace Profile.Service.Interface
{
    public interface IVideoService
    {
        Task<VideoMetadata> GetOrCreateVideoMetadata(UploadVideoChunkModel uploadVideoChunk);
    }

    internal class DefaultVideoService : IVideoService
    {
        private readonly IReadWriteRepository<IProfileEntity> _context;

        public DefaultVideoService(IReadWriteRepository<IProfileEntity> context)
        {
            _context = context;
        }

        public async Task<VideoMetadata> GetOrCreateVideoMetadata(UploadVideoChunkModel uploadVideoChunk)
        {
            var metadata = await _context.Get<VideoMetadata>()
                .FirstOrDefaultAsync(x => x.PostId == uploadVideoChunk.PostId);

            var post = await _context.Get<Post>()
                .FirstAsync(x => x.Id == uploadVideoChunk.PostId);

            _context.Attach(post);
            post.Type = PostType.Video;

            if (metadata == null)
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
                    ObjectName = string.Empty
                };
                _context.Add(metadata);
                await _context.SaveChangesAsync();
            }

            return metadata;
        }
    }
}
