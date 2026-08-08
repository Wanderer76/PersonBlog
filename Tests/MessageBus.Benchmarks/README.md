# Message bus dispatch benchmarks

Microbenchmarks for the event-dispatch hot path shared by the RabbitMQ and Kafka implementations.

The benchmark compares:

- the previous path where the consumer already knows `T`;
- the current dynamic dispatch path with envelope parsing and reflection;
- dynamic dispatch with a cached delegate;
- a single-parse alternative that deserializes only `EventData`.

Run from the repository root:

```powershell
dotnet run --project Tests/MessageBus.Benchmarks/MessageBus.Benchmarks.csproj -c Release -- --filter "*EventDispatchBenchmarks*"
```

For a quick build and execution check without statistically useful measurements:

```powershell
dotnet run --project Tests/MessageBus.Benchmarks/MessageBus.Benchmarks.csproj -c Release -- --filter "*EventDispatchBenchmarks*" --job dry
```

BenchmarkDotNet writes detailed reports to `BenchmarkDotNet.Artifacts`.
