namespace FFmpeg.Service.Models
{
    public class HlsOptions
    {
        public required IReadOnlyList<string> Resolutions { get; set; }
        public required IReadOnlyList<string> Bitrates { get; set; }
        public required IReadOnlyList<string> AudioBitrates { get; set; }
        public required string SegmentFileName { get; set; }
        public string MasterName { get; set; } = "master";
    }
}
