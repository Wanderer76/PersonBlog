namespace FFmpeg.Service.Models
{
    public class FFMpegOptions
    {
        public string DefaultEncoder { get; set; } = "h264_nvenc";//"libopenh264"; //h264_nvenc - nvidia, h264_qsv - intel, h264_amf - amd 
        public string FFMpegPath { get; set; } = "../ffmpeg/ffmpeg.exe";
        public string FFProbePath { get; set; } = "../ffmpeg/ffprobe.exe";
    }
}