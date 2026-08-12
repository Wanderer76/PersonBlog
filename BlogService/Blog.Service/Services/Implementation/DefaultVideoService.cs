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
    private readonly ICurrentUserService _currentUserService;
    public const int LifeTimeInMinutes = 60000;
    public DefaultVideoService(
        IReadWriteRepository<IBlogEntity> context,
        ICacheService cacheService,
        IFileStorageFactory fileStorageFactory,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _cacheService = cacheService;
        _fileStorageFactory = fileStorageFactory;
        _currentUserService = currentUserService;
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
    public async Task<Result<BaseFileMetadataEntity>> GetOrCreateVideoMetadata(UploadVideoChunkModel uploadVideoChunk)
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
        var currentUser = await _currentUserService.GetCurrentUserAsync();
        var post = await _context.Get<Post>()
            .Include(x => x.VideoPostInfo)
            .FirstOrDefaultAsync(x => x.Id == initiateUploadRequest.PostId);

        if (post == null)
        {
            return Result.Failure(new Error("NotFound", "Пост не найден"));
        }

        if (post.BlogId != currentUser.BlogId)
        {
            return Result.Failure(new Error("Forbidden", "Пост не принадлежит текущему пользователю"));
        }

        if (post.Type != PostType.Video)
        {
            return Result.Failure(new Error("Пост не является постом с видео"));
        }

        _context.Attach(post);
        post.ProcessState = ProcessState.Load;

        var existingFiles = await _context.Get<VideoFile>()
            .Where(x => x.PostId == post.Id)
            .ToListAsync();

        using var transaction = await _context.BeginTransactionAsync();
        if (existingFiles.Count > 0)
        {
            post.VideoPostInfo.VideoFileId = null;
            foreach (var existingFile in existingFiles)
                _context.Remove(existingFile);

            // The old rows must be deleted before the replacement is inserted,
            // otherwise the unique (PostId, Resolution, ContentType) index fails.
            await _context.SaveChangesAsync();
        }

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
        await transaction.CommitAsync();
        return Result.Success();
    }

    public async Task<Result> CompleteUploadAsync(Guid postId)
    {
        var currentUser = await _currentUserService.GetCurrentUserAsync();
        var post = await _context.Get<Post>()
            .Include(x => x.VideoPostInfo)
            .FirstOrDefaultAsync(x => x.Id == postId);

        if (post == null)
        {
            return Result.Failure(new Error("NotFound", "Пост не найден"));
        }

        if (post.BlogId != currentUser.BlogId)
        {
            return Result.Failure(new Error("Forbidden", "Пост не принадлежит текущему пользователю"));
        }

        // Complete is retried by the client when the response is lost. Once the
        // post has left the upload state, the conversion command was already saved
        // in the same unit of work as this state transition.
        if (post.ProcessState != ProcessState.Load)
        {
            return Result.Success();
        }

        var metadata = await _context.Get<VideoFile>()
                  .Where(x => x.PostId == postId)
                  .FirstOrDefaultAsync();

        if (metadata == null)
        {
            return Result.Failure(new Error("NotFound", "Метаданные видео не найдены"));
        }

        var conversionAlreadyQueued = await _context.Get<VideoProcessEvent>()
            .AnyAsync(x =>
                x.CorrelationId == metadata.Id &&
                x.EventType == nameof(ConvertVideoCommand));

        if (conversionAlreadyQueued)
        {
            _context.Attach(post);
            post.ProcessState = ProcessState.Draft;
            await _context.SaveChangesAsync();
            return Result.Success();
        }

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
        return Result.Success();
    }
}
