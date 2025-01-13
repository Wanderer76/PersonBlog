using Microsoft.EntityFrameworkCore;
using Profile.Domain.Entities;
using Profile.Service.Models.File;
using Shared.Persistence;
using Shared.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Profile.Service.Interface
{
    public interface IVideoService
    {
        public Task<VideoMetadata> GetOrCreateVideoMetadata(UploadVideoChunkModel uploadVideoChunk);

    }

    internal class VideoService : IVideoService
    {
        private readonly IReadWriteRepository<IProfileEntity> _context;

        public VideoService(IReadWriteRepository<IProfileEntity> context)
        {
            _context = context;
        }

        public async Task<VideoMetadata> GetOrCreateVideoMetadata(UploadVideoChunkModel uploadVideoChunk)
        {
            var metadata = await _context.Get<VideoMetadata>()
                .FirstOrDefaultAsync(x => x.PostId == uploadVideoChunk.PostId);
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
                    Resolution = FileStorage.Service.Models.VideoResolution.Original,
                    ObjectName = string.Empty
                };
                _context.Add(metadata);
                await _context.SaveChangesAsync();
            }

            return metadata;
        }
    }


}
