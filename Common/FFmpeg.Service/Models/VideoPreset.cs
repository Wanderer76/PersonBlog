namespace FFmpeg.Service.Models
{
    public struct VideoPreset
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public string VideoBitrate { get; set; }
        public string AudioBitrate { get; set; }

        public readonly string GetResolution() => $"{Width}x{Height}";
    }
}
