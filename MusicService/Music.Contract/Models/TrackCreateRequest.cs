using Microsoft.AspNetCore.Http;

namespace Music.Contract.Models
{
    public class TrackCreateRequest
    {
        public string Name { get; set; }
        public Guid? ArtistId { get; set; }
        public Guid? PostId { get; set; }
        public string ArtistName { get; set; }
        public Guid? AlbumId { get; set; }
        public Guid TrackFileId { get; set; }
        public Guid? ThumbnailId { get; set; }
        public short Year { get; set; }
        public List<Guid> Genres { get; set; }
    }


    public class UploadTrackFile
    {
        public Stream Stream { get; set; }
        public long Duration { get; set; }
        public string Name { get; set; }
        public string FileExtension { get; set; }
        public long Length { get; set; }
        public string ContentType { get; set; }
        public string ObjectName { get; set; }
    }

    public class UploadThumbnailFile
    {
        public Stream Stream { get; set; }
        public string Name { get; set; }
        public string FileExtension { get; set; }
        public long Length { get; set; }
        public string ContentType { get; set; }
        public string ObjectName { get; set; }
    }

}
