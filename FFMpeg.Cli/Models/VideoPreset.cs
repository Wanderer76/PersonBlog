namespace VideoProcessing.Cli.Models
{
    public struct VideoPreset
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public string VideoBitrate { get; set; }
        public string AudioBitrate { get; set; }

        public string GetResolution() => $"{Width}x{Height}";
    }
}
