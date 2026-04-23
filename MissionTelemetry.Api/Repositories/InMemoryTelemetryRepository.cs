using System.Collections.Concurrent;
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
}