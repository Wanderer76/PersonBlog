using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace FFmpeg.Service.Models
{
    public class FFProbeObject
    {
        [JsonProperty("streams")]
        public IEnumerable<FFProbeStream> Streams { get; set; }
    }

    public class FFProbeStream
    {
        [JsonProperty("codec_name")]
        public string CodecName { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }

        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("codec_type")]
        public string CodecType { get; set; }

        [JsonProperty("r_frame_rate")]
        public string RFrameRate { get; set; }

        [JsonProperty("duration")]
        public double Duration { get; set; }

        [JsonProperty("bit_rate")]
        public long BitRate { get; set; }

        [JsonProperty("index")]
        public int Index { get; set; }

        [JsonProperty("channels")]
        public int Channels { get; set; }

        [JsonProperty("sample_rate")]
        public int SampleRate { get; set; }

        [JsonProperty("pix_fmt")]
        public string PixFmt { get; set; }

        [JsonProperty("tags")]
        public Tags Tags { get; set; }

        [JsonProperty("nb_frames")]
        public string NbFrames { get; set; }

        [JsonProperty("disposition")]
        public Disposition Disposition { get; set; }
    }

    public class Tags
    {
        [JsonProperty("language")]
        public string Language { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("rotate")]
        public int? Rotate { get; set; }
    }

    public class Disposition
    {
        [JsonProperty("default")]
        public int Default { get; set; }

        [JsonProperty("forced")]
        public int Forced { get; set; }
    }
}
