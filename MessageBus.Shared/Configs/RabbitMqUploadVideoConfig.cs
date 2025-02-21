namespace MessageBus.Shared.Configs
{
    public class RabbitMqUploadVideoConfig
    {
        public string ExchangeName { get; set; }
        public string VideoProcessQueue { get; set; }
        public string VideoConverterRoutingKey { get; set; }
        public string FileChunksCombinerRoutingKey { get; set; }
        public string VideoViewRoutingKey { get; set; }
        public string ErrorRoutingKey { get; set; }
        public string VideoProcessErrorQueue { get; set; }
    }
}
