using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MissionTelemetry.Core.Models
{
    public sealed class MissionTelemetrySnapshot
    {
        public long Sequence { get; set; }
        public DateTime TimeStamp { get; set; }
        public double PowerVoltage { get; set; }
        public double PowerCurrent { get; set; }
        public double BoardTemp { get; set; }
        public double SNR {  get; set; }
        public double Roll { get; set; }
        public double Pitch {  get; set; }
        public double Yaw {  get; set; }

    }

    public sealed class MissionProximitySnapshot
    {
        public int Id { get; set; }
        public DateTime TimeStamp { get; set; }
        public double Distance_km { get; set; }
        public double Bearing_deg { get; set; }
        public double Closing_ms { get; set; }
        public double TTCA_s { get; set; }
        public double CPA_Dist_km { get; set; }
        public double TCPA_s { get; set; }
        public double X_km { get; set; }
        public double Y_km { get; set; }
        public double Vx_kms { get; set; }
        public double Vy_kms { get; set; }
    }


    public sealed class MissionAlarmSnapshot
    {
        public string Id { get; set; } = "";
        public string Key { get; set; } = "";
        public Severity Severity { get; set; }
        public string Message { get; set; } = "";
        public double LastValue { get; set; }
        public DateTime LastSeen { get; set; }
        public bool IsAcknowledged { get; set; }
        public bool IsLatched { get; set; }
        public int Count { get; set; }
    }

    public sealed class MissionSnapshot
    {
        public DateTime GeneratedAtUtc { get; set; }
        public bool IsRunning { get; set; }
        public Severity HighestSeverity { get; set; }
        public int ActiveAlarmCount { get; set; }

        public IReadOnlyList<MissionTelemetrySnapshot> Telemetry {  get; set; } = Array.Empty<MissionTelemetrySnapshot>();
        public IReadOnlyList<MissionProximitySnapshot> Proximity { get; set; } = Array.Empty<MissionProximitySnapshot>();
        public IReadOnlyList<MissionAlarmSnapshot> ActiveAlarms { get; set;} = Array.Empty<MissionAlarmSnapshot>();
    }
}
