using Microsoft.EntityFrameworkCore;
using MissionTelemetry.Core.Models;
using MissionTelemetry.Persistence;

namespace MissionTelemetry.Api.Repositories;

public sealed class EfTelemetryRepository : ITelemetryRepository
{
    private readonly MissionDbContext _db;

    public EfTelemetryRepository(MissionDbContext db)
        => _db = db;

    public void Add(TelemetryFrame frame)
    {
        // absichtlich leer, da SimulationWorker direkt in EF schreibt
    }

    public IReadOnlyList<TelemetryFrame> GetLatest(int take)
        => QueryFrames(skip: 0, take: take);

    public IReadOnlyList<TelemetryFrame> GetRange(int skip, int take)
        => QueryFrames(skip, take);

    public int Count
        => _db.TelemetrySamples
              .AsNoTracking()
              .Select(s => s.TimeStamp)
              .Distinct()
              .Count();

    public void Clear()
    {
        _db.TelemetrySamples.ExecuteDelete();
    }

    public IReadOnlyList<string> GetKeys()
    {
        return _db.TelemetrySamples
            .AsNoTracking()
            .Select(s => s.Key)
            .Distinct()
            .OrderBy(k => k)
            .ToList();
    }

    private IReadOnlyList<TelemetryFrame> QueryFrames(int skip, int take)
    {
        take = Math.Clamp(take, 1, 500);
        skip = Math.Max(0, skip);

        var stamps = _db.TelemetrySamples
            .AsNoTracking()
            .OrderByDescending(s => s.TimeStamp)
            .Select(s => s.TimeStamp)
            .Distinct()
            .Skip(skip)
            .Take(take)
            .ToList();

        if (stamps.Count == 0)
            return Array.Empty<TelemetryFrame>();

        var samples = _db.TelemetrySamples
            .AsNoTracking()
            .Where(s => stamps.Contains(s.TimeStamp))
            .OrderByDescending(s => s.TimeStamp)
            .ThenBy(s => s.Key)
            .ToList();

        var frames = samples
            .GroupBy(s => s.TimeStamp)
            .OrderByDescending(g => g.Key)
            .Select((g, idx) =>
            {
                var dict = new Dictionary<string, double>(StringComparer.Ordinal);
                foreach (var s in g)
                    dict[s.Key] = s.Value;

                return new TelemetryFrame
                {
                    Sequence = idx,
                    TimeStamp = g.Key,
                    Values = dict
                };
            })
            .ToList();

        return frames;
    }

    public IReadOnlyList<(DateTime TimeStamp, double Value)> GetByKey(string key, int take)
    {
        take = Math.Clamp(take, 1, 1000);

        return _db.TelemetrySamples
            .AsNoTracking()
            .Where(s => s.Key == key)
            .OrderByDescending(s => s.TimeStamp)
            .Take(take)
            .OrderBy(s => s.TimeStamp)
            .Select(s => new ValueTuple<DateTime, double>(s.TimeStamp, s.Value))
            .ToList();
    }
}