using System.Collections.Concurrent;

namespace EventGateway.Api.Metrics;

public class MetricsRegistry
{
    private readonly ConcurrentDictionary<string, (long Count, double TotalMs)> _metrics = new();

    public void Record(string endpoint, double elapsedMs)
    {
        _metrics.AddOrUpdate(endpoint,
            _ => (1, elapsedMs),
            (_, existing) => (existing.Count + 1, existing.TotalMs + elapsedMs));
    }

    public object Snapshot()
    {
        return _metrics.Select(kvp => new
        {
            Endpoint = kvp.Key,
            Count = kvp.Value.Count,
            AvgLatencyMs = Math.Round(kvp.Value.TotalMs / kvp.Value.Count, 2)
        }).ToList();
    }
}