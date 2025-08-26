using Music.Contract.Models;
using Shared.Utils;

namespace Music.Domain.Services;

public interface IArtistService
{
    Task<Result> CreateArtistFromExistBlogAsync(Guid blogId);
    Task<Result> CreateNewArtistAsync(CreateArtistRequest createArtistRequest);
    Task<Result> AddTrackToArtistAsync(Guid artistId, Guid trackId);
    Task<Result> RemoveTrackFormArtistAsync(Guid artistId, Guid trackId);
}