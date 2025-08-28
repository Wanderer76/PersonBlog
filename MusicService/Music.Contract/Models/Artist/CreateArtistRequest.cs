namespace Music.Contract.Models.Artist
{
    public class CreateArtistRequest
    {
        public required string ArtistName { get; set; }
        public Guid ThumbnailId { get; set; }
    }
}
