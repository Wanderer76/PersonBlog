namespace MessageBus.Configs
{
    public class RabbitMqConfig
    {
        public string HostName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public int Port { get; set; }
        public string ExchangeName { get; set; }
        public string VideoProcessQueue { get; set; }
        public string VideoConverterRoutingKey { get; set; }
        public string FileChunksCombinerRoutingKey { get; set; }
        public string VideoViewRoutingKey { get; set; }
    }
}
