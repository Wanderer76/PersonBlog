using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using MessageBus.Models;

namespace MessageBus.Benchmarks;

[MemoryDiagnoser]
public class EventDispatchBenchmarks
{
    private static readonly JsonSerializerOptions DeserializeOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IReadOnlyDictionary<string, Type> _eventTypes =
        new Dictionary<string, Type>
        {
            [nameof(ReactionEvent)] = typeof(ReactionEvent),
            [nameof(ViewEvent)] = typeof(ViewEvent)
        };

    private readonly ConcurrentDictionary<string, Func<string, object?>> _cachedDispatchers = new();
    private string _message = null!;

    [GlobalSetup]
    public void Setup()
    {
        var reaction = new ReactionEvent
        {
            EventId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            PostId = Guid.NewGuid(),
            RemoteIp = "127.0.0.1",
            Time = 42.5,
            IsLike = true
        };

        _message = JsonSerializer.Serialize(BaseEvent<ReactionEvent>.Create(reaction));
        _cachedDispatchers.Clear();
        _cachedDispatchers[nameof(ReactionEvent)] = CreateDispatcher(typeof(ReactionEvent));
    }

    [Benchmark(Baseline = true, Description = "Known generic event type (previous path)")]
    public object? KnownGenericType()
    {
        return JsonSerializer.Deserialize<BaseEvent<ReactionEvent>>(
            _message,
            DeserializeOptions)?.EventData;
    }

    [Benchmark(Description = "Dynamic type + reflection (current path)")]
    public object? DynamicReflection()
    {
        using var document = JsonDocument.Parse(_message);
        var eventType = document.RootElement
            .GetProperty(nameof(BaseEvent.EventType))
            .GetString()!;
        var payloadType = _eventTypes[eventType];

        var method = typeof(EventDispatchBenchmarks)
            .GetMethod(nameof(DeserializeTyped), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(payloadType);

        return method.Invoke(null, [_message]);
    }

    [Benchmark(Description = "Dynamic type + cached delegate")]
    public object? CachedDispatcher()
    {
        using var document = JsonDocument.Parse(_message);
        var eventType = document.RootElement
            .GetProperty(nameof(BaseEvent.EventType))
            .GetString()!;

        var dispatcher = _cachedDispatchers.GetOrAdd(
            eventType,
            static (key, state) => CreateDispatcher(state[key]),
            _eventTypes);

        return dispatcher(_message);
    }

    [Benchmark(Description = "Single JSON parse + runtime payload type")]
    public object? SingleParseRuntimeType()
    {
        using var document = JsonDocument.Parse(_message);
        var root = document.RootElement;
        var eventType = root
            .GetProperty(nameof(BaseEvent.EventType))
            .GetString()!;

        return root
            .GetProperty(nameof(BaseEvent<object>.EventData))
            .Deserialize(_eventTypes[eventType], DeserializeOptions);
    }

    private static Func<string, object?> CreateDispatcher(Type payloadType)
    {
        var method = typeof(EventDispatchBenchmarks)
            .GetMethod(nameof(DeserializeTyped), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(payloadType);

        return method.CreateDelegate<Func<string, object?>>();
    }

    private static object? DeserializeTyped<T>(string message)
    {
        var envelope = JsonSerializer.Deserialize<BaseEvent<T>>(
            message,
            DeserializeOptions);

        return envelope is null ? null : envelope.EventData;
    }

    private sealed class ReactionEvent
    {
        public Guid EventId { get; init; }
        public Guid? UserId { get; init; }
        public Guid PostId { get; init; }
        public string? RemoteIp { get; init; }
        public double Time { get; init; }
        public bool? IsLike { get; init; }
    }

    private sealed class ViewEvent
    {
        public Guid EventId { get; init; }
        public Guid? UserId { get; init; }
        public Guid PostId { get; init; }
        public double WatchedTime { get; init; }
    }
}
