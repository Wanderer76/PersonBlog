using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Microsoft.Extensions.Hosting;

internal sealed class ProcessMetrics : IHostedService, IDisposable
{
    public const string MeterName = "PersonBlog.Process";

    private readonly object _cpuLock = new();
    private readonly Meter _meter = new(MeterName);
    private readonly ObservableGauge<long> _memoryUsage;
    private readonly ObservableGauge<double> _cpuUtilization;
    private long _lastCpuTimestamp = Stopwatch.GetTimestamp();
    private TimeSpan _lastCpuTime = Process.GetCurrentProcess().TotalProcessorTime;
    private double _lastCpuUtilization;

    public ProcessMetrics()
    {
        _memoryUsage = _meter.CreateObservableGauge(
            "process.memory.usage",
            ObserveMemoryUsage,
            unit: "By",
            description: "The amount of physical memory in use by the process.");

        _cpuUtilization = _meter.CreateObservableGauge(
            "process.cpu.utilization",
            ObserveCpuUtilization,
            unit: "1",
            description: "The process CPU utilization from 0 to 1, normalized by processor count.");
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public void Dispose()
    {
        _meter.Dispose();
    }

    private static long ObserveMemoryUsage()
    {
        using var process = Process.GetCurrentProcess();
        return process.WorkingSet64;
    }

    private double ObserveCpuUtilization()
    {
        lock (_cpuLock)
        {
            var currentTimestamp = Stopwatch.GetTimestamp();
            var elapsedSeconds = (currentTimestamp - _lastCpuTimestamp) / (double)Stopwatch.Frequency;

            // Seq and Aspire can collect almost simultaneously. Reuse the last
            // value instead of reporting a misleading zero to the second exporter.
            if (elapsedSeconds < 1)
            {
                return _lastCpuUtilization;
            }

            using var process = Process.GetCurrentProcess();
            var currentCpuTime = process.TotalProcessorTime;
            var cpuSeconds = (currentCpuTime - _lastCpuTime).TotalSeconds;

            _lastCpuTimestamp = currentTimestamp;
            _lastCpuTime = currentCpuTime;
            _lastCpuUtilization = Math.Clamp(
                cpuSeconds / (elapsedSeconds * Environment.ProcessorCount),
                0,
                1);

            return _lastCpuUtilization;
        }
    }
}
