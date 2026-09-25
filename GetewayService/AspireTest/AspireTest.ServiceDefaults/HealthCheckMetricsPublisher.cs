using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.Hosting;

internal sealed class HealthCheckMetricsPublisher : IHealthCheckPublisher, IDisposable
{
    public const string MeterName = "PersonBlog.HealthChecks";

    private readonly ConcurrentDictionary<string, HealthStatus> _statuses = new();
    private readonly Meter _meter;
    private readonly ObservableGauge<double> _healthGauge;

    public HealthCheckMetricsPublisher(IHostEnvironment environment)
    {
        _meter = new Meter(MeterName);
        _healthGauge = _meter.CreateObservableGauge(
            "service.health",
            ObserveHealthChecks,
            description: "Current health-check state: healthy=1, degraded=0.5, unhealthy=0.");
    }

    public Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
    {
        _statuses["overall"] = report.Status;

        foreach (var entry in report.Entries)
        {
            _statuses[entry.Key] = entry.Value.Status;
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _meter.Dispose();
    }

    private IEnumerable<Measurement<double>> ObserveHealthChecks()
    {
        foreach (var (checkName, status) in _statuses)
        {
            yield return new Measurement<double>(
                ToMetricValue(status),
                new KeyValuePair<string, object?>("health.check.name", checkName),
                new KeyValuePair<string, object?>("health.status", status.ToString().ToLowerInvariant()));
        }
    }

    private static double ToMetricValue(HealthStatus status) => status switch
    {
        HealthStatus.Healthy => 1,
        HealthStatus.Degraded => 0.5,
        _ => 0
    };
}
