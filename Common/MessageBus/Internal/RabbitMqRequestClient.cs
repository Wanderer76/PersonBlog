using MessageBus.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace MessageBus.Internal;

internal sealed class RabbitMqRequestClient : IRequestClient, IAsyncDisposable
{
    private readonly Lazy<Task<IConnection>> _connectionLazy;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<byte[]>> _pendingRequests = new();
    private readonly ConcurrentDictionary<Type, EventPublishAttribute> _cachedAttributes = new();

    private string? _replyQueueName;
    private IChannel? _replyChannel;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    private static readonly JsonSerializerOptions _deserializeOptions = new() { PropertyNameCaseInsensitive = true };

    public RabbitMqRequestClient(Lazy<Task<IConnection>> connectionLazy)
    {
        _connectionLazy = connectionLazy;
    }

    private async Task EnsureReplyQueueAsync()
    {
        if (_replyQueueName != null) return;

        await _initLock.WaitAsync();
        try
        {
            if (_replyQueueName != null) return;

            var connection = await _connectionLazy.Value;
            _replyChannel = await connection.CreateChannelAsync();

            // Создаем эксклюзивную, автоудаляемую очередь для ответов
            var ok = await _replyChannel.QueueDeclareAsync("", false, true, true);
            _replyQueueName = ok.QueueName;

            var consumer = new AsyncEventingBasicConsumer(_replyChannel);
            consumer.ReceivedAsync += OnReplyReceived;

            await _replyChannel.BasicConsumeAsync(_replyQueueName, autoAck: true, consumer: consumer);
        }
        finally
        {
            _initLock.Release();
        }
    }

    private Task OnReplyReceived(object sender, BasicDeliverEventArgs ea)
    {
        var correlationId = ea.BasicProperties.CorrelationId;
        if (!string.IsNullOrEmpty(correlationId) && _pendingRequests.TryRemove(correlationId, out var tcs))
        {
            tcs.TrySetResult(ea.Body.ToArray());
        }
        return Task.CompletedTask;
    }

    public async Task<TResponse> RequestAsync<TRequest, TResponse>(
        string exchangeName, string routingKey, TRequest request,
        TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        await EnsureReplyQueueAsync();

        var requestCorrelationId = Guid.NewGuid().ToString();
        var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);

        _pendingRequests[requestCorrelationId] = tcs;

        try
        {
            var connection = await _connectionLazy.Value;
            using var channel = await connection.CreateChannelAsync();

            var baseEvent = BaseEvent<TRequest>.Create(request);
            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(baseEvent));

            var properties = new BasicProperties
            {
                CorrelationId = requestCorrelationId,
                ReplyTo = _replyQueueName,
                Persistent = true
            };

            await channel.BasicPublishAsync(
                exchange: exchangeName,
                routingKey: routingKey,
                mandatory: true,
                basicProperties: properties,
                body: body);

            var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(30);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(effectiveTimeout);

            using var registration = cts.Token.Register(() => tcs.TrySetCanceled(cts.Token));

            var responseBytes = await tcs.Task;

            var responseEvent = JsonSerializer.Deserialize<BaseEvent<TResponse>>(responseBytes, _deserializeOptions);
            if (responseEvent == null)
                throw new InvalidOperationException("Failed to deserialize response event");

            return responseEvent.EventData;
        }
        finally
        {
            _pendingRequests.TryRemove(requestCorrelationId, out _);
        }
    }

    public async Task<TResponse> RequestAsync<TRequest, TResponse>(
        TRequest request, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        var type = typeof(TRequest);
        var attr = _cachedAttributes.GetOrAdd(type, t => t.GetCustomAttribute<EventPublishAttribute>(false));

        if (attr == null || string.IsNullOrEmpty(attr.Exchange))
            throw new InvalidOperationException($"EventPublishAttribute with Exchange is required for {type.Name}");

        return await RequestAsync<TRequest, TResponse>(attr.Exchange, attr.RoutingKey, request, timeout, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_replyChannel != null && _replyChannel.IsOpen)
        {
            try
            {
                await _replyChannel.CloseAsync();
                await _replyChannel.DisposeAsync();
            }
            catch { /* ignore */ }
        }
        _initLock.Dispose();
    }
}