namespace FileStorage.Service.Models
{
    public class HlsOptions
    {
        public required IReadOnlyList<string> Resolutions { get; init; }
        public required IReadOnlyList<string> Bitrates { get; init; }
        public required IReadOnlyList<string> AudioBitrates { get; init; }
        public required string SegmentFileName { get; init; }
        public required string MasterName { get; init; } = "master";
        public required string EncodePreset { get; init; } = "fast";
    }
}
