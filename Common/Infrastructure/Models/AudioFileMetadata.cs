namespace Infrastructure.Models
{
    public class AudioFileMetadata
    {
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }
        public string Year { get; set; }
        public string Genre { get; set; }
        public long Duration { get; set; }
        public string Bitrate { get; set; }
        public bool HasCover { get; set; }
        public string CoverBase64 { get; set; }
        public string CoverMimeType { get; set; }
        public string OriginalFileName { get; set; }
        public long FileSize { get; set; }
    }
}
