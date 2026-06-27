namespace MessageBus.Configs;

public sealed class KafkaConnection
{
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string? SaslUsername { get; set; }
    public string? SaslPassword { get; set; }
    public string? SecurityProtocol { get; set; } // PLAINTEXT, SASL_SSL
    public string? SaslMechanism { get; set; }    // PLAIN, SCRAM-SHA-256

    // Настройки producer
    public int MessageTimeoutMs { get; set; } = 5000;
    public int Retries { get; set; } = 3;
    public string Acks { get; set; } = "all";

    // Настройки consumer
    public int SessionTimeoutMs { get; set; } = 10000;
    public int MaxPollIntervalMs { get; set; } = 300000;
    public bool EnableAutoCommit { get; set; } = false;
    public string AutoOffsetReset { get; set; } = "Earliest";
}