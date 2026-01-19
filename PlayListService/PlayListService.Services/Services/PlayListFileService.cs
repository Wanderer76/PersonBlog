using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using PlayListService.Domain.Entities;
using Shared.Models;
using Shared.Persistence;
using Shared.Services;

namespace PlayListService.Services.Services;

public sealed class PlayListFileService : IDisposable
{
    private readonly IFileStorage fileStorage;
    private readonly IReadWriteRepository<IPlayListEntity> writeRepository;

    public PlayListFileService(IFileStorageFactory fileStorageFactory, IReadWriteRepository<IPlayListEntity> writeRepository)
    {
        fileStorage = fileStorageFactory.CreateFileStorage();
        this.writeRepository = writeRepository;
    }

    public async Task<string?> GetThumbnailAsync(PlayList playList)
    {
        if (playList.ThumbnailId == null) return null;
        return await GetThumbnailAsync(playList.UserId, playList.ThumbnailId.Value);
    }

    public async Task<string?> GetThumbnailAsync(Guid userId, Guid thumbnailId)
    {
        var thumbnailName = await writeRepository.Get<PlayListFile>()
            .FirstAsync(x => x.Id == thumbnailId);
        var url = await fileStorage.GetFileUrlAsync(userId, thumbnailName.ObjectName);
        return url;
    }

    public async Task<FileMetadata> UploadThumbnailAsync(Guid playlistId, Guid creatorId, FileMetadata fileMetadata, Stream input)
    {
        var id = GuidService.GetNewGuid();
        var file = new PlayListFile
        {
            Id = fileMetadata.Id,
            ContentType = fileMetadata.ContentType,
            CreatedAt = fileMetadata.CreatedAt,
            FileExtension = fileMetadata.FileExtension,
            Length = fileMetadata.Length,
            Name = fileMetadata.Name,
            PlaylistId = playlistId,
            ObjectName = $"playLists/thumbnail/{id}",
        };
        writeRepository.Add(file);
        await fileStorage.PutFileAsync(creatorId, file.ObjectName, input);
        await writeRepository.SaveChangesAsync();
        return file;
    }

    public async Task<FileMetadata> UploadThumbnailWithoutPlayListAsync(Guid creatorId, FileMetadata fileMetadata, Stream input)
    {
        var id = GuidService.GetNewGuid();
        var file = new PlayListFile
        {
            Id = fileMetadata.Id,
            ContentType = fileMetadata.ContentType,
            CreatedAt = fileMetadata.CreatedAt,
            FileExtension = fileMetadata.FileExtension,
            Length = fileMetadata.Length,
            Name = fileMetadata.Name,
            ObjectName = $"playLists/thumbnail/{id}",
        };
        writeRepository.Add(file);
        var url = await fileStorage.PutFileAsync(creatorId, file.ObjectName, input);
        await writeRepository.SaveChangesAsync();
        return fileMetadata;
    }


    public async Task RemoveThumbnailAsync(PlayList playList)
    {
        if (playList.ThumbnailId == null) return;

        var thumbnail = await writeRepository.Get<PlayListFile>()
            .FirstAsync(x => x.PlaylistId == playList.Id);
        writeRepository.Remove(thumbnail);
        await fileStorage.RemoveFileAsync(playList.UserId, thumbnail.ObjectName);
        await writeRepository.SaveChangesAsync();
    }

    public void Dispose()
    {
        fileStorage?.Dispose();
    }
}
