using Shared.Utils;

namespace Music.Domain.Services
{
    public interface IArtistService
    {
        Task<Result> CreateArtistFromExistBlogAsync(Guid blogId);
        Task<Result> CreateNewArtistAsync(CreateArtistRequest createArtistRequest);
    }

    public class CreateArtistRequest
    {
        public required string ArtistName { get; set; }
        public string ThumbnailUrl {  get; set; }
    }
}
