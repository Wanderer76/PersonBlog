using MessageBus.Models;
using Shared.Services;

namespace MessageBus;

public interface IMessagePublish : IHave<IRequestClient>
{
    //[Obsolete("Неудачное решение",true)]
    //Task PublishAsync<T>(string exchangeName, string routingKey, T message, MessageProperty? cfg = null);
    Task PublishAsync<T>(BaseEvent<T> message, MessageProperty? cfg = null);
    Task PublishAsync(BaseEvent message, MessageProperty? cfg = null);
}

public interface IRequestClient
{
    Task<TResponse> RequestAsync<TRequest, TResponse>(TRequest request, TimeSpan? timeout = null, CancellationToken cancellationToken = default);
}

public static class RequestClientExtension
{
    public static Task<TResponse> RequestAsync<TRequest, TResponse>(this IHave<IRequestClient> client,
        TRequest request,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        return client.Get<IRequestClient>().RequestAsync<TRequest, TResponse>(request, timeout, cancellationToken);
    }
}

public interface IMessageSubscriber : IAsyncDisposable
{
    Task InitializeSubscriptionAsync(CancellationToken cancellationToken);
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
[Obsolete("Неудачное решение")]
public class EventPublishAttribute : Attribute
{
    public string Exchange { get; set; }
    public string RoutingKey { get; set; }
}

public class MessageProperty
{
    public bool Persistence { get; set; } = true;
    public string CorrelationId { get; set; }
    public string RoutingKey { get; set; }
    public string Exchange { get; set; }
}