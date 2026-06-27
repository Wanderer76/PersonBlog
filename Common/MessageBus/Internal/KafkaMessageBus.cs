using Confluent.Kafka;
using Confluent.Kafka.Admin;
using MessageBus.Configs;
using MessageBus.EventHandler;
using MessageBus.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace MessageBus.Internal;

internal sealed class KafkaMessageBus : IMessagePublish, IMessageSubscriber
{
    private readonly KafkaConnection _config;
    private readonly IServiceScopeFactory _serviceScope;
    private readonly MessageBusSubscriptionInfo _subscriptionInfo;
    private readonly ConcurrentDictionary<Type, EventPublishAttribute> _cachedAttributes = new();
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _consumers = new();
    private readonly ProducerBuilder<string, string> _producerBuilder;

    private static readonly JsonSerializerOptions _deserializeOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    private static readonly JsonSerializerOptions _baseEventSerializerOptions = new()
    {
        Converters = { new BaseEventJsonConverter() }
    };

    private const string ErrorTopicSuffix = ".error";
    private const string CorrelationIdHeader = "correlation-id";

    public KafkaMessageBus(
        KafkaConnection config,
        IServiceScopeFactory serviceScope,
        IOptions<MessageBusSubscriptionInfo> subscriptionInfo)
    {
        _config = config;
        _serviceScope = serviceScope;
        _subscriptionInfo = subscriptionInfo.Value;

        _producerBuilder = new ProducerBuilder<string, string>(BuildProducerConfig());
    }

    private ProducerConfig BuildProducerConfig()
    {
        var cfg = new ProducerConfig
        {
            BootstrapServers = _config.BootstrapServers,
            MessageTimeoutMs = _config.MessageTimeoutMs,
            Acks = (Acks)Enum.Parse(typeof(Acks), _config.Acks, true),
            EnableIdempotence = true,
        };
        ApplySecurity(cfg);
        return cfg;
    }

    private ConsumerConfig BuildConsumerConfig(string groupId)
    {
        var cfg = new ConsumerConfig
        {
            BootstrapServers = _config.BootstrapServers,
            GroupId = groupId,
            EnableAutoCommit = _config.EnableAutoCommit,
            AutoOffsetReset = Enum.Parse<AutoOffsetReset>(_config.AutoOffsetReset, true),
            SessionTimeoutMs = _config.SessionTimeoutMs,
            MaxPollIntervalMs = _config.MaxPollIntervalMs,
            // Аналог BasicQos(prefetchCount)
            
        };
        ApplySecurity(cfg);
        return cfg;
    }

    private void ApplySecurity(ClientConfig cfg)
    {
        if (!string.IsNullOrEmpty(_config.SecurityProtocol))
            cfg.SecurityProtocol = Enum.Parse<SecurityProtocol>(_config.SecurityProtocol, true);
        if (!string.IsNullOrEmpty(_config.SaslMechanism))
            cfg.SaslMechanism = Enum.Parse<SaslMechanism>(_config.SaslMechanism, true);
        if (!string.IsNullOrEmpty(_config.SaslUsername))
            cfg.SaslUsername = _config.SaslUsername;
        if (!string.IsNullOrEmpty(_config.SaslPassword))
            cfg.SaslPassword = _config.SaslPassword;
    }

    #region IMessageSubscriber

    public async Task InitializeSubscriptionAsync(CancellationToken cancellationToken)
    {
        // Создаём топики для ошибок (аналог exchange "error" в RabbitMQ)
        await EnsureTopicExistsAsync(GetErrorTopicName(), cancellationToken);

        foreach (var handlerConfig in _subscriptionInfo.Handlers)
        {
            await InitializeSubscriptionAsync(handlerConfig, cancellationToken);
        }
    }

    private async Task InitializeSubscriptionAsync(
        HandlerInfo handlerConfig,
        CancellationToken cancellationToken)
    {
        try
        {
            var topicName = ResolveTopicName(handlerConfig);
            var groupId = ResolveGroupId(handlerConfig);
            var prefetch = handlerConfig.Queue?.PrefetchCount ?? 10;

            await EnsureTopicExistsAsync(topicName, cancellationToken);

            var subscribeMethod = typeof(KafkaMessageBus)
                .GetMethod(nameof(StartConsumerAsync), BindingFlags.NonPublic | BindingFlags.Instance)!;
            var genericSubscribe = subscribeMethod
                .MakeGenericMethod(handlerConfig.HandlerType);

            await (Task)genericSubscribe.Invoke(this, [topicName, groupId, prefetch, cancellationToken])!;
        }
        catch (Exception ex)
        {
            // Логируем, но не роняем весь сервис
            // logger.LogError(ex, "Failed to initialize subscription for {HandlerType}", handlerConfig.HandlerType);
        }
    }

    private string ResolveTopicName(HandlerInfo handlerConfig)
    {
        var attr = handlerConfig.HandlerType.GetCustomAttribute<EventPublishAttribute>(false);

        // Приоритет: Exchange из атрибута → Exchange из Queue → имя типа
        return attr?.Exchange
            ?? handlerConfig.Queue?.Exchange?.Name
            ?? handlerConfig.HandlerType.Name;
    }

    private string ResolveGroupId(HandlerInfo handlerConfig)
    {
        return handlerConfig.Queue?.QueueName
            ?? handlerConfig.HandlerType.FullName!;
    }

    private async Task EnsureTopicExistsAsync(string topicName, CancellationToken ct)
    {
        using var admin = new AdminClientBuilder(new AdminClientConfig
        {
            BootstrapServers = _config.BootstrapServers
        }).Build();

        try
        {
            await admin.CreateTopicsAsync(new[]
            {
                new TopicSpecification
                {
                    Name = topicName,
                    NumPartitions = 1,
                    ReplicationFactor = 1
                }
            });
        }
        catch (CreateTopicsException ex) when (
            ex.Results.Any(r => r.Error.Code == ErrorCode.TopicAlreadyExists))
        {
            // Топик уже существует — это нормально
        }
    }

    private async Task StartConsumerAsync<T>(
        string topicName,
        string groupId,
        int prefetch,
        CancellationToken externalCt)
    {
        if (_consumers.ContainsKey(topicName + ":" + groupId))
            return;

        var cts = CancellationTokenSource.CreateLinkedTokenSource(externalCt);
        _consumers[topicName + ":" + groupId] = cts;

        var config = BuildConsumerConfig(groupId);

        _ = Task.Run(async () =>
        {
            using var consumer = new ConsumerBuilder<string, string>(config)
                .SetErrorHandler((_, e) => { /* logger.LogError(...) */ })
                .Build();

            consumer.Subscribe(topicName);

            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        var result = consumer.Consume(cts.Token);
                        if (result?.Message == null) continue;

                        await ProcessMessageAsync<T>(consumer, result, cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (ConsumeException ex)
                    {
                        // logger.LogError(ex, "Kafka consume error on {Topic}", topicName);
                    }
                }
            }
            finally
            {
                try { consumer.Close(); } catch { /* ignore */ }
            }
        }, cts.Token);

        await Task.CompletedTask;
    }

    private async Task ProcessMessageAsync<T>(
        IConsumer<string, string> consumer,
        ConsumeResult<string, string> result,
        CancellationToken ct)
    {
        try
        {
            var concreteEvent = JsonSerializer.Deserialize<BaseEvent<T>>(
                result.Message.Value, _deserializeOptions);

            if (concreteEvent == null)
            {
                // Не валидное сообщение — коммитим, чтобы не зациклиться
                consumer.Commit(result);
                return;
            }

            using var scope = _serviceScope.CreateScope();
            var handlers = scope.ServiceProvider
                .GetKeyedServices<IEventHandler<T>>(concreteEvent.EventType);

            if (!handlers.Any())
            {
                consumer.Commit(result);
                return;
            }

            Guid? correlationId = TryGetCorrelationId(result.Message.Headers);
            var context = MessageContext.Create(correlationId, concreteEvent.EventData, this);

            foreach (var handler in handlers)
            {
                try
                {
                    await handler.Handle(context);
                }
                catch (TargetInvocationException tie) when (tie.InnerException != null)
                {
                    await PublishErrorAsync(concreteEvent, tie.InnerException);
                    consumer.Commit(result); // Коммитим, чтобы не requeue
                    return;
                }
                catch (Exception e)
                {
                    await PublishErrorAsync(concreteEvent, e);
                    consumer.Commit(result);
                    return;
                }
            }

            // Успешная обработка — коммитим offset (аналог BasicAck)
            consumer.Commit(result);
        }
        catch (Exception ex)
        {
            // Критическая ошибка — коммитим, чтобы избежать зацикливания
            try { consumer.Commit(result); } catch { /* ignore */ }
            // logger.LogError(ex, "Critical error processing message");
        }
    }

    private static Guid? TryGetCorrelationId(Headers? headers)
    {
        if (headers == null) return null;
        var header = headers.FirstOrDefault(h => h.Key == CorrelationIdHeader);
        if (header?.GetValueBytes() == null) return null;

        var value = Encoding.UTF8.GetString(header.GetValueBytes());
        return Guid.TryParse(value, out var id) ? id : null;
    }

    private async Task PublishErrorAsync<T>(BaseEvent<T> originalEvent, Exception error)
    {
        try
        {
            var errorBody = JsonSerializer.Serialize(new
            {
                OriginalEvent = originalEvent,
                Error = error.ToString(),
                Timestamp = DateTimeOffset.UtcNow
            });

            using var producer = _producerBuilder.Build();
            await producer.ProduceAsync(
                GetErrorTopicName(),
                new Message<string, string>
                {
                    Key = originalEvent.EventType,
                    Value = errorBody
                });
        }
        catch { /* ignore DLQ errors to avoid infinite loops */ }
    }

    private static string GetErrorTopicName() => "message-bus.errors";

    #endregion

    #region IMessagePublish

    public async Task SendMessageAsync<T>(string exchangeName, string routingKey, T message)
        where T : BaseEvent
    {
        // exchangeName в Kafka = topicName
        var topic = !string.IsNullOrEmpty(exchangeName) ? exchangeName : routingKey;

        using var producer = _producerBuilder.Build();
        var headers = new Headers();
        if (message.CorrelationId.HasValue)
        {
            headers.Add(CorrelationIdHeader,
                Encoding.UTF8.GetBytes(message.CorrelationId.Value.ToString()));
        }

        await producer.ProduceAsync(topic, new Message<string, string>
        {
            Key = message.EventType,
            Value = JsonSerializer.Serialize(message),
            Headers = headers
        });
    }

    public async Task PublishAsync<T>(
        string exchangeName,
        string routingKey,
        T message,
        MessageProperty? cfg = null)
    {
        cfg ??= new MessageProperty();
        var topic = !string.IsNullOrEmpty(exchangeName) ? exchangeName : routingKey;

        var baseEvent = BaseEvent<T>.Create(message);
        var body = JsonSerializer.Serialize(baseEvent);

        using var producer = _producerBuilder.Build();
        var headers = BuildHeaders(cfg.CorrelationId);

        await producer.ProduceAsync(topic, new Message<string, string>
        {
            Key = typeof(T).Name,
            Value = body,
            Headers = headers
        });
    }

    public async Task PublishAsync<T>(BaseEvent<T> message, MessageProperty? cfg = null)
    {
        cfg ??= new MessageProperty();
        ConfigureProperties<T>(cfg);

        var topic = cfg.Exchange ?? message.EventType;
        var body = JsonSerializer.Serialize(message);

        using var producer = _producerBuilder.Build();
        var headers = BuildHeaders(cfg.CorrelationId);

        await producer.ProduceAsync(topic, new Message<string, string>
        {
            Key = message.EventType,
            Value = body,
            Headers = headers
        });
    }

    public async Task PublishAsync(BaseEvent message, MessageProperty? cfg = null)
    {
        cfg ??= new MessageProperty();

        var type = _subscriptionInfo.EventTypes[message.EventType];
        var attr = _cachedAttributes.GetOrAdd(type,
            t => t.GetCustomAttribute<EventPublishAttribute>(false));
        var handlerConfig = _subscriptionInfo.Handlers
            .FirstOrDefault(x => x.HandlerType == type);

        cfg.RoutingKey ??= attr?.RoutingKey ?? handlerConfig?.Queue?.Exchange?.RoutingKey;
        cfg.Exchange ??= attr?.Exchange ?? handlerConfig?.Queue?.Exchange?.Name;

        var topic = cfg.Exchange ?? message.EventType;
        var body = JsonSerializer.Serialize(message, _baseEventSerializerOptions);

        using var producer = _producerBuilder.Build();
        var headers = BuildHeaders(cfg.CorrelationId);

        await producer.ProduceAsync(topic, new Message<string, string>
        {
            Key = message.EventType,
            Value = body,
            Headers = headers
        });
    }

    private static Headers BuildHeaders(string? correlationId)
    {
        var headers = new Headers();
        if (!string.IsNullOrEmpty(correlationId))
        {
            headers.Add(CorrelationIdHeader, Encoding.UTF8.GetBytes(correlationId));
        }
        return headers;
    }

    private void ConfigureProperties<T>(MessageProperty cfg)
    {
        var type = typeof(T);
        var attr = _cachedAttributes.GetOrAdd(type,
            t => t.GetCustomAttribute<EventPublishAttribute>(false));
        var handlerConfig = _subscriptionInfo.Handlers
            .FirstOrDefault(x => x.HandlerType == type);

        cfg.RoutingKey ??= attr?.RoutingKey ?? handlerConfig?.Queue?.Exchange?.RoutingKey;
        cfg.Exchange ??= attr?.Exchange ?? handlerConfig?.Queue?.Exchange?.Name;
    }

    #endregion

    #region IAsyncDisposable

    public async ValueTask DisposeAsync()
    {
        // 1. Останавливаем всех консьюмеров
        foreach (var (_, cts) in _consumers)
        {
            try { cts.Cancel(); cts.Dispose(); } catch { /* ignore */ }
        }
        _consumers.Clear();

        // Небольшая пауза, чтобы консьюмеры корректно закрылись
        await Task.Delay(100);

        await ValueTask.CompletedTask;
    }

    #endregion
}