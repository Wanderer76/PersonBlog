using MessageBus.Configs;
using MessageBus.EventHandler;
using MessageBus.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Services;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace MessageBus.Internal;

internal sealed class RabbitMqMessageBus : IAsyncDisposable, IMessagePublish, IMessageSubscriber
{
    private readonly IConnectionFactory _factory;
    private readonly IServiceScopeFactory _serviceScope;
    private readonly Lazy<Task<IConnection>> _connectionLazy;
    private readonly MessageBusSubscriptionInfo _subscriptionInfo;
    private readonly ConcurrentDictionary<Type, EventPublishAttribute> _cachedValues;

    private readonly ConcurrentDictionary<string, SubscriptionContext> _subscriptions = new();

    private static readonly JsonSerializerOptions _baseEventSerializerOptions = new() { Converters = { new BaseEventJsonConverter() } };
    private static readonly JsonSerializerOptions _deserializeOptions = new() { PropertyNameCaseInsensitive = true };

    public RabbitMqMessageBus(
        RabbitMqConnection config,
        IServiceScopeFactory serviceScope,
        IOptions<MessageBusSubscriptionInfo> subscriptionInfo)
    {
        _factory = new ConnectionFactory
        {
            HostName = config.HostName,
            Port = config.Port,
            UserName = config.UserName,
            Password = config.Password,
        };
        _subscriptionInfo = subscriptionInfo.Value;
        _serviceScope = serviceScope;
        _cachedValues = new();

        // Ленивая инициализация ОСНОВНОГО соединения (для публикации/подписки)
        _connectionLazy = new Lazy<Task<IConnection>>(async () =>
        {
            try
            {
                return await _factory.CreateConnectionAsync();
            }
            catch (Exception ex)
            {
                // Logger?.LogError(ex, "Failed to connect to RabbitMQ at {Host}:{Port}", config.HostName, config.Port);
                throw;
            }
        });
    }

    private Task<IConnection> GetConnectionInternalAsync() => _connectionLazy.Value;

    #region IHostedService

    public async Task InitializeSubscriptionAsync(CancellationToken cancellationToken)
    {
        _ = await GetConnectionInternalAsync();

        using var initConnection = await _factory.CreateConnectionAsync();
        using var initChannel = await initConnection.CreateChannelAsync(cancellationToken: cancellationToken);
        foreach (var handlerConfig in _subscriptionInfo.Handlers)
        {
            await InitializeSubscriptionAsync(initChannel, handlerConfig, cancellationToken);
        }
    }

    private async Task InitializeSubscriptionAsync(IChannel initChannel, HandlerInfo handlerConfig, CancellationToken cancellationToken)
    {
        try
        {
            var queue = handlerConfig.Queue;
            var attributeData = handlerConfig.HandlerType.GetCustomAttribute<EventPublishAttribute>(false);
            var queueName = queue?.QueueName ?? handlerConfig.HandlerType.FullName!;

            // Объявляем очередь
            await initChannel.QueueDeclareAsync(
                queue: queueName,
                durable: queue?.Durable ?? true,
                exclusive: queue?.Exclusive ?? false,
                autoDelete: queue?.AutoDelete ?? false,
                cancellationToken: cancellationToken);

            // Объявляем обменник и привязку (если нужно)
            var exchange = queue?.Exchange;
            var exchangeName = exchange?.Name ?? attributeData?.Exchange;

            if (!string.IsNullOrEmpty(exchangeName))
            {
                var exchangeType = exchange?.ExchangeType
                    ?? ((attributeData?.RoutingKey != null || exchange?.RoutingKey != null)
                        ? ExchangeType.Direct : ExchangeType.Fanout);

                await initChannel.ExchangeDeclareAsync(
                    exchange: exchangeName,
                    type: exchangeType,
                    durable: exchange?.Durable ?? true,
                    autoDelete: exchange?.AutoDelete ?? false,
                    cancellationToken: cancellationToken);

                await initChannel.QueueBindAsync(
                    queue: queueName,
                    exchange: exchangeName,
                    routingKey: exchange?.RoutingKey ?? attributeData?.RoutingKey,
                    cancellationToken: cancellationToken);
            }

            var subscribeMethod = typeof(RabbitMqMessageBus).GetMethod(nameof(StartConsumerAsync), BindingFlags.NonPublic | BindingFlags.Instance)!;
            var genericSubscribe = subscribeMethod.MakeGenericMethod(handlerConfig.HandlerType);

            await (Task)genericSubscribe.Invoke(this, [queueName])!;
        }
        catch (Exception ex)
        {
            // ❗ Логируем, но не роняем весь сервис из-за одной очереди
            // logger.LogError(ex, "Failed to initialize subscription for {HandlerType}", handlerConfig.HandlerType);
        }
    }

    #endregion

    #region Consumer Management

    private async Task StartConsumerAsync<T>(string queueName)
    {
        // Проверяем, что подписка ещё не активна (защита от дублей)
        if (_subscriptions.ContainsKey(queueName))
            return;

        var connection = await GetConnectionInternalAsync();
        var channel = await connection.CreateChannelAsync();

        // Настройка DLQ для ошибок (опционально)
        await SetupErrorHandlingAsync(channel);

        await channel.BasicQosAsync(0, 10, false);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (model, ea) => await ProcessMessageAsync<T>(channel, ea);

        var consumerTag = await channel.BasicConsumeAsync(queueName, autoAck: false, consumer: consumer);

        // Сохраняем контекст для управления жизненным циклом
        _subscriptions[queueName] = new SubscriptionContext(channel, consumer, consumerTag);
    }

    private async Task SetupErrorHandlingAsync(IChannel channel)
    {
        await channel.ExchangeDeclareAsync("error", ExchangeType.Fanout, true, false);
        await channel.QueueDeclareAsync("errors", true, false, false);
        await channel.QueueBindAsync("errors", "error", "");
    }

    private async Task ProcessMessageAsync<T>(IChannel channel, BasicDeliverEventArgs ea)
    {
        try
        {
            // Десериализуем конкретное событие из EventData
            var concreteEvent = JsonSerializer.Deserialize<BaseEvent<T>>(ea.Body.Span, _deserializeOptions);
            if (concreteEvent == null)
            {
                await RejectMessageAsync(channel, ea.DeliveryTag, requeue: false);
                return;
            }

            using var scope = _serviceScope.CreateScope();

            // 🔍 Находим все хендлеры для этого типа события: IEventHandler<TConcrete>
            var handlers = scope.ServiceProvider.GetKeyedServices<IEventHandler<T>>(concreteEvent.EventType);

            if (!handlers.Any())
            {
                // Нет обработчиков — не рекуеим, т.к. это системная ошибка
                await RejectMessageAsync(channel, ea.DeliveryTag, requeue: false);
                return;
            }

            Guid? correlationId = string.IsNullOrWhiteSpace(ea.BasicProperties.CorrelationId)
            ? null
            : Guid.Parse(ea.BasicProperties.CorrelationId);

            var context = MessageContext.Create(correlationId, concreteEvent.EventData, this);

            foreach (var handler in handlers)
            {
                try
                {
                    await handler.Handle(context);
                }
                catch (TargetInvocationException tie) when (tie.InnerException != null)
                {
                    await PublishErrorAsync(channel, concreteEvent, tie.InnerException);
                    await RejectMessageAsync(channel, ea.DeliveryTag, requeue: false);
                    return;
                }
                catch (Exception e)
                {
                    await PublishErrorAsync(channel, concreteEvent, e);
                    await RejectMessageAsync(channel, ea.DeliveryTag, requeue: false);
                    return;
                }
            }

            await channel.BasicAckAsync(ea.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            // Критическая ошибка — не рекуеим, чтобы избежать зацикливания
            await RejectMessageAsync(channel, ea.DeliveryTag, requeue: false);
            // Логируем ex
        }
    }

    private async Task PublishErrorAsync<T>(IChannel channel, BaseEvent<T> originalEvent, Exception error)
    {
        try
        {
            var errorBody = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
            {
                OriginalEvent = originalEvent,
                Error = error.ToString(),
                Timestamp = DateTimeService.Now()
            }));
            await channel.BasicPublishAsync("error", "", true, new BasicProperties(), errorBody);
        }
        catch { /* ignore DLQ errors to avoid infinite loops */ }
    }

    private async Task RejectMessageAsync(IChannel channel, ulong deliveryTag, bool requeue)
    {
        try
        {
            await channel.BasicNackAsync(deliveryTag, false, requeue);
        }
        catch { /* ignore */ }
    }

    #endregion

    #region IMessagePublish

    public async Task SendMessageAsync<T>(string exchangeName, string routingKey, T message) where T : BaseEvent
    {
        var connection = await GetConnectionInternalAsync();
        using var channel = await connection.CreateChannelAsync();

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        await channel.BasicPublishAsync(
            exchange: exchangeName,
            routingKey: routingKey,
            mandatory: true,
            basicProperties: new BasicProperties
            {
                Persistent = true,
                CorrelationId = message.CorrelationId?.ToString(),
            },
            body: body);
    }

    public async Task PublishAsync<T>(string exchangeName, string routingKey, T message, MessageProperty? cfg = null)
    {
        cfg ??= new MessageProperty();
        var connection = await GetConnectionInternalAsync();
        using var channel = await connection.CreateChannelAsync();

        var baseEvent = BaseEvent<T>.Create(message);
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(baseEvent));

        await channel.BasicPublishAsync(
            exchange: exchangeName,
            routingKey: routingKey,
            mandatory: true,
            basicProperties: new BasicProperties
            {
                CorrelationId = cfg.CorrelationId,
                Persistent = cfg.Persistence,
            },
            body: body);
    }

    public async Task PublishAsync<T>(BaseEvent<T> message, MessageProperty? cfg = null)
    {
        cfg ??= new MessageProperty();
        ConfigureProperties<T>(cfg);

        var connection = await GetConnectionInternalAsync();
        using var channel = await connection.CreateChannelAsync();

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        await channel.BasicPublishAsync(
            exchange: cfg.Exchange,
            routingKey: cfg.RoutingKey,
            mandatory: true,
            basicProperties: new BasicProperties
            {
                CorrelationId = cfg.CorrelationId,
                Persistent = cfg.Persistence,
            },
            body: body);
    }

    public async Task PublishAsync(BaseEvent message, MessageProperty? cfg = null)
    {
        cfg ??= new MessageProperty();

        var type = _subscriptionInfo.EventTypes[message.EventType];
        var attr = _cachedValues.GetOrAdd(type, t => t.GetCustomAttribute<EventPublishAttribute>(false));
        var handlerConfig = _subscriptionInfo.Handlers.FirstOrDefault(x => x.HandlerType == type);

        cfg.RoutingKey ??= attr?.RoutingKey ?? handlerConfig?.Queue?.Exchange?.RoutingKey;
        cfg.Exchange ??= attr?.Exchange ?? handlerConfig?.Queue?.Exchange?.Name;

        var connection = await GetConnectionInternalAsync();
        using var channel = await connection.CreateChannelAsync();

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message, _baseEventSerializerOptions));
        await channel.BasicPublishAsync(
            exchange: cfg.Exchange,
            routingKey: cfg.RoutingKey,
            mandatory: true,
            basicProperties: new BasicProperties
            {
                CorrelationId = cfg.CorrelationId,
                Persistent = cfg.Persistence,
            },
            body: body);
    }

    private void ConfigureProperties<T>(MessageProperty cfg)
    {
        var type = typeof(T);
        var attr = _cachedValues.GetOrAdd(type, t => t.GetCustomAttribute<EventPublishAttribute>(false));
        var handlerConfig = _subscriptionInfo.Handlers.FirstOrDefault(x => x.HandlerType == type);

        cfg.RoutingKey ??= attr?.RoutingKey ?? handlerConfig?.Queue?.Exchange?.RoutingKey;
        cfg.Exchange ??= attr?.Exchange ?? handlerConfig?.Queue?.Exchange?.Name;
    }

    #endregion

    #region IAsyncDisposable

    public async ValueTask DisposeAsync()
    {
        // 1. Останавливаем всех консьюмеров
        foreach (var (queueName, context) in _subscriptions)
        {
            try
            {
                await context.Channel.BasicCancelAsync(context.ConsumerTag);
                if (context.Channel.IsOpen) await context.Channel.CloseAsync();
                await context.Channel.DisposeAsync();
            }
            catch { /* ignore */ }
        }
        _subscriptions.Clear();

        // 2. Закрываем основное соединение (если было создано)
        if (_connectionLazy.IsValueCreated)
        {
            try
            {
                var connection = await _connectionLazy.Value;
                if (connection.IsOpen) await connection.CloseAsync();
                await connection.DisposeAsync();
            }
            catch { /* ignore */ }
        }
    }

    #endregion

    private record SubscriptionContext(IChannel Channel, AsyncEventingBasicConsumer Consumer, string ConsumerTag);
}