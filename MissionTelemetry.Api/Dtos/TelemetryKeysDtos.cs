namespace MissionTelemetry.Api.Dtos
{
    public sealed class TelemetryValuePointDto
    {
        public DateTime TimeStamp { get; set; }
        public double Value { get; set; }
    }

    public sealed class TelemetryKeySeriesDto
    {
        public string Key { get; set; } = "";
        public int Count {  get; set; } 
        public IReadOnlyList<TelemetryValuePointDto> Items { get; set; } = Array.Empty<TelemetryValuePointDto>();
    }
}
