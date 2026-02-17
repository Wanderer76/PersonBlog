namespace FFmpeg.Service.Models
{
    public class HlsVideoPresets
    {
        public List<VideoPreset> VideoPresets { get;set; }
        public string EncodePreset { get; set; } = "ultrafast"; //medium
    }
}
