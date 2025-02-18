namespace FFmpeg.Service.Models
{
    public class HlsOptions
    {
        public required string[] Resolutions { get; set; }
        public required string[] Bitrates { get; set; }
        public required string[] AudioBitrates { get; set; }
        public required string SegmentFileName { get; set; }
        public string MasterName { get; set; } = "master";
    }
}
