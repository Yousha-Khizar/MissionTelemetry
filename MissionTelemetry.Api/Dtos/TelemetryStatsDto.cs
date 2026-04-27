namespace MissionTelemetry.Api.Dtos
{
    public sealed class TelemetryStatsDto
    {
        public string Key { get; set; }
        public int Count { get; set; }
        public double Latest { get; set; }
        public double Min {  get; set; }
        public double Max { get; set; }
        public double Average { get; set; }
    }
}
