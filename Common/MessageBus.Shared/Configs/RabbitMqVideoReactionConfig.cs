namespace MessageBus.Shared.Configs
{
    public static class RabbitMqVideoReactionConfig
    {
        public const string ExchangeName = "video-reacting";
        public const string QueueName = "video-reactions";
        public const string SyncQueueName = "video-sync";
        public const string ViewRoutingKey = "video.viewed";
        public const string SyncRoutingKey = "video.sync";
    }
}
