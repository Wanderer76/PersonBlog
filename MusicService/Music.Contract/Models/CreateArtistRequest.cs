namespace Music.Contract.Models
{
    public class CreateArtistRequest
    {
        public required string ArtistName { get; set; }
        public string ThumbnailUrl { get; set; }
    }
}
