using MissionTelemetry.Core.Models;

namespace MissionTelemetry.Api.Repositories;

public interface ITelemetryRepository
{
    void Add(TelemetryFrame frame);
    IReadOnlyList<TelemetryFrame> GetLatest(int take);
    IReadOnlyList<TelemetryFrame> GetRange(int skip, int take);
    void Clear();
    int Count { get; }

    IReadOnlyList<string> GetKeys();
}

