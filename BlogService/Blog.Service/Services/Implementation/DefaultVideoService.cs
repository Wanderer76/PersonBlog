using Amazon.Runtime.Internal;
using Blog.Contracts.Events;
using Blog.Contracts.Models.File;
using Blog.Contracts.Services;
using Blog.Domain.Entities;
using FileStorage.Service.Models;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Models;
using Shared.Persistence;
using Shared.Services;
using Shared.Utils;

namespace Blog.Service.Services.Implementation;
internal sealed class DefaultVideoService : IVideoService
{
    private readonly IReadWriteRepository<IBlogEntity> _context;
    private readonly ICacheService _cacheService;
    private readonly IFileStorageFactory _fileStorageFactory;
    public const int LifeTimeInMinutes = 60000;
    public DefaultVideoService(IReadWriteRepository<IBlogEntity> context, ICacheService cacheService, IFileStorageFactory fileStorageFactory)
    {
        _context = context;
        _cacheService = cacheService;
        _fileStorageFactory = fileStorageFactory;
    }
    [Obsolete("", true)]
    public async Task<Result<UploadVideoProgress>> CreateUploadVideoMetadata(CreateUploadVideoProgressRequest uploadVideoChunk)
    {
        var isExits = await _context.Get<Post>()
            .Where(x => x.Id == uploadVideoChunk.PostId)
            .AnyAsync();

        if (!isExits)
        {
            return new Error("Не удалось найти пост");
        }

        var progress = new UploadVideoProgress(GuidService.GetNewGuid())
        {
            PostId = uploadVideoChunk.PostId,
            LastUploadChunkNumber = 0,
            TotalChunkCount = uploadVideoChunk.TotalChunkCount,
            TotalSize = uploadVideoChunk.TotalSize
        };

        return await _cacheService.GetOrAddDataAsync(progress, () => Task.FromResult(progress), LifeTimeInMinutes);
    }

    [Obsolete("", true)]
    public async Task<Result<FileMetadata>> GetOrCreateVideoMetadata(UploadVideoChunkModel uploadVideoChunk)
    {
        var progress = await GetUploadVideoMetadata(uploadVideoChunk.FileId);

        if (progress == null)
        {
            return new Error("Не удалось получить данные о прогрессе загрузки");
        }

        var cacheKey = new VideoMetadataCacheKey(uploadVideoChunk.PostId);

        var cacheResult = await _cacheService.GetCachedDataAsync<VideoFile>(cacheKey);

        if (cacheResult != null)
        {
            return cacheResult;
        }

        var post = await _context.Get<Post>()
            .FirstAsync(x => x.Id == uploadVideoChunk.PostId);

        if (post.Type != PostType.Video)
        {
            return new Error("Пост не является постом с видео");
        }

        _context.Attach(post);
        post.ProcessState = ProcessState.Load;

        var metadata = new VideoFile
        {
            Id = progress.Value.FileId,
            FileExtension = uploadVideoChunk.FileExtension,
            CreatedAt = DateTimeService.Now(),
            ContentType = uploadVideoChunk.ContentType,
            PostId = uploadVideoChunk.PostId,
            Name = uploadVideoChunk.FileName,
            Resolution = VideoResolution.Original,
            Duration = uploadVideoChunk.Duration,
            ObjectName = string.Empty,
            Length = uploadVideoChunk.TotalSize
        };

        using var fileStorage = _fileStorageFactory.CreateFileStorage();
        await fileStorage.CreateTempBucketAsync(post.Id);
        await _cacheService.SetCachedDataAsync(cacheKey, metadata, TimeSpan.FromMinutes(LifeTimeInMinutes));
        await _context.SaveChangesAsync();
        return metadata;
    }
    
    [Obsolete("", true)]
    public async Task<Result<UploadVideoProgress>> GetUploadVideoMetadata(Guid fileId)
    {
        var data = await _cacheService.GetCachedDataAsync<UploadVideoProgress>(new UploadVideoProgress(fileId));
        if (data == null)
        {
            return new Error("Не удалось найти данные о загрузке файла");
        }
        return data;
    }

    public async Task<Result> InitVideoUploadAsync(InitiateUploadRequest initiateUploadRequest)
    {
        var post = await _context.Get<Post>()
            .FirstAsync(x => x.Id == initiateUploadRequest.PostId);

        if (post.Type != PostType.Video)
        {
            return Result.Failure(new Error("Пост не является постом с видео"));
        }

        var exists = await _context.Get<VideoFile>()
            .FirstOrDefaultAsync(x => x.PostId == post.Id);

        if (exists != null)
        {
            _context.Attach(exists);
            exists.PostId = Guid.Empty;
        }

        _context.Attach(post);
        post.ProcessState = ProcessState.Load;

        var metadata = new VideoFile
        {
            Id = GuidService.GetNewGuid(),
            FileExtension = initiateUploadRequest.FileExtension,
            CreatedAt = DateTimeService.Now(),
            ContentType = initiateUploadRequest.ContentType,
            PostId = initiateUploadRequest.PostId,
            Name = initiateUploadRequest.FileName,
            Resolution = VideoResolution.Original,
            Duration = initiateUploadRequest.Duration,
            ObjectName = initiateUploadRequest.ObjectName,
            Length = initiateUploadRequest.Size,
        };
        _context.Add(metadata);
        await _context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task CompleteUploadAsync(Guid postId)
    {
        var metadata = await _context.Get<VideoFile>()
                  .Where(x => x.PostId == postId)
                  .FirstAsync();


        var post = await _context.Get<Post>()
            .Include(x => x.VideoPostInfo)
            .FirstAsync(x => x.Id == metadata.PostId);

        var videoCreateEvent = new ConvertVideoCommand
        {
            VideoMetadata = metadata,
            HasPreviewId = post.VideoPostInfo.PreviewId.HasValue,
            ObjectName = metadata.ObjectName,
            BlogId = post.BlogId,
            VideoMetadataId = metadata.Id,
            PostId = metadata.PostId,
        };

        _context.Attach(post);

        var videoEvent = VideoProcessEvent.Create(videoCreateEvent, videoCreateEvent.VideoMetadataId);
        post.ProcessState = ProcessState.Draft;
        _context.Add(videoEvent);
        await _context.SaveChangesAsync();
    }
}
