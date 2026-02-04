using Blog.Domain.Entities;
using Blog.Service.Models.File;
using FileStorage.Service.Models;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
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

    public async Task<Result<VideoFile>> GetOrCreateVideoMetadata(UploadVideoChunkModel uploadVideoChunk)
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

    public async Task<Result<UploadVideoProgress>> GetUploadVideoMetadata(Guid fileId)
    {
        var data = await _cacheService.GetCachedDataAsync<UploadVideoProgress>(new UploadVideoProgress(fileId));
        if (data == null)
        {
            return new Error("Не удалось найти данные о загрузке файла");
        }
        return data;
    }

    public async Task<Result> CreateFileMetadataAsync(InitiateUploadRequest initiateUploadRequest)
    {
        var post = await _context.Get<Post>()
            .FirstAsync(x => x.Id == initiateUploadRequest.PostId);

        if (post.Type != PostType.Video)
        {
            return Result.Failure(new Error("Пост не является постом с видео"));
        }

        var exists  = await _context.Get<VideoFile>()
            .FirstOrDefaultAsync(x=>x.PostId == post.Id);

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
            Length = initiateUploadRequest.Size
        };
        _context.Add(metadata);
        await _context.SaveChangesAsync();
        return Result.Success();
    }
}
public class InitiateUploadRequest
{
    public Guid PostId { get; set; }
    public string ObjectName { get; set; } = null!;
    public long Size { get; set; }
    public string ContentType { get; set; }= null!;
    public string FileExtension { get; set; } = null!;
    public string FileName { get; set; } = null!;
    public double Duration { get; set; }
}