namespace MessageBus.Shared.Configs
{
    public class RabbitMqVideoReactionConfig
    {
        public string ExchangeName { get; set; } = "video-reaction";
        public string QueueName { get; set; } = "video-reactions";
        public string SyncQueueName { get; set; } = "video-sync";
        public string ViewRoutingKey { get; set; } = "video.viewed";
        public string SyncRoutingKey { get; set; } = "video.sync";
    }
}
