using System;
using System.Collections.Generic;

namespace MissionTelemetry.Api.Dtos;

public sealed class TelemetryFrameDto
{
    public long Sequence {get; set;}
    
    public DateTime TimeStamp {get; set;}
    public int ValueCount {get; set;}
    public Dictionary<string, double> Values {get; set;} = new();
}

public sealed class TelemetryPageDto
{
    public long Total {get; set;}
    public int Skip {get; set;}
    public int Take {get; set;}
    public IReadOnlyList<TelemetryFrameDto> Items {get; set;} = Array.Empty<TelemetryFrameDto>();
}