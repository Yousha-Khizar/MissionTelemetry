using System.Collections.Concurrent;
using MissionTelemetry.Api.Dtos;
using MissionTelemetry.Core.Models;

namespace MissionTelemetry.Api.Repositories;

public sealed class InMemoryTelemetryRepository : ITelemetryRepository
{
    private readonly ConcurrentQueue<TelemetryFrame> _queue = new();
    private readonly int _max = 10_000;

    public int Count => _queue.Count;

    public void Add(TelemetryFrame frame)
    {
        _queue.Enqueue(frame);
        while (_queue.Count > _max && _queue.TryDequeue(out _)) { }
    }

    public IReadOnlyList<TelemetryFrame> GetLatest(int take)
        => _queue.Reverse().Take(take).Reverse().ToList();

    public IReadOnlyList<TelemetryFrame> GetRange(int skip, int take)
        => _queue.Skip(Math.Max(0, skip)).Take(Math.Clamp(take, 0, 1000)).ToList();

    public void Clear()
    {
        while (_queue.TryDequeue(out _)) { }
    }

    public IReadOnlyList<string> GetKeys()
    {
        return _queue
            .Where(f => f.Values is not null)
            .SelectMany(f => f.Values.Keys)
            .Distinct()
            .OrderBy(k => k)
            .ToList();
    }

    public IReadOnlyList<(DateTime TimeStamp, double Value)> GetByKey(string key, int take)
    {
        take = Math.Clamp(take, 1, 1000);

        return _queue
            .Where(f => f.Values is not null && f.Values.ContainsKey(key))
            .Reverse()
            .Take(take)
            .Reverse()
            .Select(f => (f.TimeStamp, f.Values[key]))
            .ToList();

    }

    public TelemetryStatsDto? GetStats(string key, int take)
    {
        take = Math.Clamp(take, 1, 1000);

        var values = _queue
            .Where(f => f.Values is not null && f.Values.ContainsKey(key))
            .Reverse()
            .Take(take)
            .Reverse()
            .Select(f => f.Values[key])
            .ToList();

        if (values.Count == 0)
            return null;

        return new TelemetryStatsDto
        {
            Key = key,
            Count = values.Count,
            Latest = values[^1],
            Min = values.Min(),
            Max = values.Max(),
            Average = values.Average()
        };
    }
}