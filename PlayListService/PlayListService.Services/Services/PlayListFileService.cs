using Infrastructure.Services;
using PlayListService.Domain.Entities;

namespace PlayListService.Services.Services;

public sealed class PlayListFileService : IDisposable
{
    private readonly IFileStorage fileStorage;

    public PlayListFileService(IFileStorageFactory fileStorageFactory)
    {
        fileStorage = fileStorageFactory.CreateFileStorage();
    }

    public async Task<string?> GetThumbnailAsync(PlayList playList)
    {
        if (playList.ThumbnailId == null) return null;
        return await GetThumbnailAsync(playList.UserId, playList.ThumbnailId);
    }

    public async Task<string?> GetThumbnailAsync(Guid userId, string thumbnailId)
    {
        var url = await fileStorage.GetFileUrlAsync(userId, thumbnailId);
        return url;
    }

    public async Task<string?> UploadThumbnailAsync(Guid playlistId, Guid creatorId, Stream input)
    {
        var url = await fileStorage.PutFileAsync(creatorId, $"playLists/thumbnail/{playlistId}", input);
        return url;
    }

    public async Task RemoveThumbnailAsync(PlayList playList)
    {
        if (playList.ThumbnailId == null) return;
        await fileStorage.RemoveFileAsync(playList.UserId, playList.ThumbnailId);
    }

    public void Dispose()
    {
        fileStorage?.Dispose();
    }
}
