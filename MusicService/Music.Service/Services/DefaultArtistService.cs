using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Music.Contract.Models.Artist;
using Music.Domain.Entities;
using Music.Domain.Services;
using Shared.Persistence;
using Shared.Utils;

namespace Music.Service.Services;

internal class DefaultArtistService : IArtistService
{
    private readonly IReadWriteRepository<IMusicEntity> _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFileStorageFactory _fileStorageFactory;

    public DefaultArtistService(IReadWriteRepository<IMusicEntity> repository, ICurrentUserService currentUserService, IFileStorageFactory fileStorageFactory)
    {
        _repository = repository;
        _currentUserService = currentUserService;
        _fileStorageFactory = fileStorageFactory;
    }

    public Task<Result> AddTrackToArtistAsync(Guid artistId, Guid trackId)
    {
        throw new NotImplementedException();
    }

    public Task<Result> CreateArtistFromExistBlogAsync(Guid blogId)
    {
        throw new NotImplementedException();
    }

    public async Task<Result> CreateNewArtistAsync(CreateArtistRequest createArtistRequest)
    {
        var user = await _currentUserService.GetCurrentUserAsync();

        if (await _repository.Get<Artist>().AnyAsync(x => x.UserId == user.UserId.Value))
        {
            return Result.Failure(new Error("Artist for user already exist"));
        }
        var artist = Artist.CreateForUser(createArtistRequest.ArtistName, user.UserId.Value, createArtistRequest.ThumbnailId);
        _repository.Add(artist);
        await _repository.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<ArtistDetailView>> GetCurrentUserArtistAsync()
    {
        var user = await _currentUserService.GetCurrentUserAsync();
        var artist = await _repository.Get<Artist>()
            .Include(x => x.AvatarMetadata)
            .FirstOrDefaultAsync(x => x.UserId == user.UserId.Value);

        if (artist == null)
        {
            return Result<ArtistDetailView>.Failure(new Error("Not found"));
        }

        var trackCount = await _repository.Get<ArtistTrackLink>()
           .Where(x => x.ArtistId == artist.Id)
           .CountAsync();

        var uploadedTracksCount = await _repository.Get<Track>()
            .Where(x => x.UploadedByUserId == user.UserId.Value)
            .CountAsync();

        using var storage = _fileStorageFactory.CreateFileStorage();

        var avatarUrl = artist.AvatarMetadata == null
            ? null
            : await storage.GetFileUrlAsync(artist.AvatarMetadata.Id, artist.AvatarMetadata.ObjectName);

        return new ArtistDetailView(artist.Id, artist.Name, avatarUrl, trackCount + uploadedTracksCount);
    }

    public Task<Result> RemoveTrackFormArtistAsync(Guid artistId, Guid trackId)
    {
        throw new NotImplementedException();
    }
}
