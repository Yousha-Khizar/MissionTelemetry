using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using MissionTelemetry.Core.Infrastructure;
using MissionTelemetry.Core.Models;
using MissionTelemetry.Core.Services;
using static System.Net.Mime.MediaTypeNames;

namespace MissionTelemetry.Core.ViewModels;

public sealed class TelemetryRow : ObservableObject
{
    public long Sequence { get; init; }
    public DateTime TimeStamp { get; init; }
    public double PowerVoltage { get; init; }
    public double PowerCurrent { get; init; }
    public double BoardTemp { get; init; }
    public double SNR { get; init; }
    public double Roll { get; init; }
    public double Pitch { get; init; }
    public double Yaw { get; init; }
}

public sealed class ProximityRow : ObservableObject
{
    public int Id { get; init; }
    public DateTime TimeStamp { get; init; }
    public double Distance_km { get; init; }
    public double Bearing_deg { get; init; }
    public double Closing_ms { get; init; }   // Annäherung positiv
    public double TTCA_s { get; init; }       // grob
    public double CPA_Dist_km { get; init; }  // exakter
    public double TCPA_s { get; init; }       // Zeit bis CPA
    public double X_km { get; init; }
    public double Y_km { get; init; }
    public double Vx_kms { get; init; }
    public double Vy_kms { get; init; }
}

public sealed class TelemetryViewModel : ObservableObject
{
    private readonly ITelemtrySource _source;
    private readonly IAlarmEvaluator _evaluator;
    private readonly IProximitySource _prox;
    private readonly IAlarmManager _alarms;

    private const int MaxRows = 500;

    public ObservableCollection<TelemetryRow> Frames { get; } = new();
    public ObservableCollection<ProximityRow> Proximity { get; } = new();
    public ReadOnlyObservableCollection<ActiveAlarm> ActiveAlarms => _alarms.Active;

    public RelayCommand StartCommand { get; }
    public RelayCommand StopCommand { get; }
    public RelayCommand ClearCommand { get; }
    public RelayCommand AckAllCommand { get; }

    private ActiveAlarm? _selectedAlarm;
    public ActiveAlarm? SelectedAlarm
    {
        get => _selectedAlarm;
        set { if (Set(ref _selectedAlarm, value)) AckSelectedCommand.RaiseCanExecuteChanged(); }
    }
    public RelayCommand AckSelectedCommand { get; }

    private bool _isRunning;
    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (Set(ref _isRunning, value))
            { StartCommand.RaiseCanExecuteChanged(); StopCommand.RaiseCanExecuteChanged(); }
        }
    }

    private double _radarRangeKm = 30.0;
    public double RadarRangeKm { get => _radarRangeKm; set => Set(ref _radarRangeKm, value); }

    public Severity HighestSeverity =>
        ActiveAlarms.Any(a => a.Severity == Severity.Alarm) ? Severity.Alarm :
        ActiveAlarms.Any(a => a.Severity == Severity.Warning) ? Severity.Warning :
        Severity.Info;

    public TelemetryViewModel(ITelemtrySource source, IAlarmEvaluator evaluator, IProximitySource prox, IAlarmManager alarms)
    {
        _source = source; _evaluator = evaluator; _prox = prox; _alarms = alarms;

        _ui = SynchronizationContext.Current ?? new SynchronizationContext();

        _source.FrameReceived += OnFrame;
        _prox.Snapshot += OnProximity;

        

        StartCommand = new RelayCommand(Start, () => !IsRunning);
        StopCommand = new RelayCommand(Stop, () => IsRunning);
        ClearCommand = new RelayCommand(() =>
        {
            Frames.Clear();
            Proximity.Clear();
            _alarms.AcknowledgeAll();
            OnPropertyChanged(nameof(HighestSeverity));
        });
        AckAllCommand = new RelayCommand(() =>
        {
            _alarms.AcknowledgeAll();
            OnPropertyChanged(nameof(HighestSeverity));
        });
        AckSelectedCommand = new RelayCommand(() =>
        {
            if (SelectedAlarm is { } a)
            {
                _alarms.Acknowledge(a.Id);
                OnPropertyChanged(nameof(HighestSeverity));
            }
        }, () => SelectedAlarm != null);

        Start();
    }

    private void Start() { _source.Start(); _prox.Start(); IsRunning = true; }
    private void Stop() { _source.Stop(); _prox.Stop(); IsRunning = false; }

    
    private void OnFrame(object? _, TelemetryFrame frame)
    {
        _ui.Post(_ =>
        {
            var row = new TelemetryRow
            {
                Sequence = frame.Sequence,
                TimeStamp = frame.TimeStamp,
                PowerVoltage = frame.Values.GetValueOrDefault("Power.Voltage"),
                PowerCurrent = frame.Values.GetValueOrDefault("Power.Current"),
                BoardTemp = frame.Values.GetValueOrDefault("Thermal.BoardTemp"),
                SNR = frame.Values.GetValueOrDefault("Comms.SNR"),
                Roll = frame.Values.GetValueOrDefault("Attitude.Roll"),
                Pitch = frame.Values.GetValueOrDefault("Attitude.Pitch"),
                Yaw = frame.Values.GetValueOrDefault("Attitude.Yaw"),
            };

            Frames.Insert(0, row);
            while (Frames.Count > MaxRows) Frames.RemoveAt(Frames.Count - 1);

            // Regeln anwenden → aktive Alarme aktualisieren
            foreach (var ev in _evaluator.Evaluate(frame))
                _alarms.RaiseOrUpdate(ev.Key, ev.Severity, ev.Message, ev.Value, latched: false);

            // Entwarnung: nicht-latched Alarme wieder entfernen
            foreach (var rule in _evaluator.Rules)
                if (!rule.IsTriggered(frame.Values.GetValueOrDefault(rule.Key)))
                    _alarms.ClearIfNotLatched(rule.Key);

            OnPropertyChanged(nameof(HighestSeverity));
        }, null);
    }

    // Radialmetrik (Distanz, Bearing, Closing, TTCA)
    private static (double dist_km, double bearing_deg, double closing_ms, double ttca_s) Radial(ProximityContact c)
    {
        double dist = Math.Sqrt(c.X_km * c.X_km + c.Y_km * c.Y_km);
        double bearing = (Math.Atan2(c.Y_km, c.X_km) * 180.0 / Math.PI + 360) % 360;
        double rdotv = c.X_km * c.Vx_kms + c.Y_km * c.Vy_kms;
        double closing_kms = -(dist > 1e-9 ? rdotv / dist : 0.0);
        double closing_ms = closing_kms * 1000.0;
        double ttca_s = (closing_ms > 0.0) ? (dist * 1000.0 / closing_ms) : double.PositiveInfinity;
        return (dist, bearing, closing_ms, ttca_s);
    }

    // CPA/TCPA (Closest Point of Approach)
    private static (double cpaDist_km, double tcpa_s) CPA(ProximityContact c)
    {
        double rx = c.X_km, ry = c.Y_km, vx = c.Vx_kms, vy = c.Vy_kms;
        double vv = vx * vx + vy * vy;
        if (vv < 1e-12) return (Math.Sqrt(rx * rx + ry * ry), double.PositiveInfinity);
        double t = -(rx * vx + ry * vy) / vv;
        if (t < 0) t = 0; // nur Zukunft
        double cx = rx + vx * t, cy = ry + vy * t;
        return (Math.Sqrt(cx * cx + cy * cy), t);
    }

    // Proximity-Snapshot verarbeiten, latched Proximity-Alarme setzen
    private void OnProximity(object? _, ProximitySnapshot snap)
    {
        _ui.Post(_ =>
        {
            Proximity.Clear();

            foreach (var c in snap.Contacts)
            {
                var (d, b, cr, ttca) = Radial(c);
                var (cpaD, tcpa) = CPA(c);

                Proximity.Add(new ProximityRow
                {
                    Id = c.Id,
                    TimeStamp = snap.TimeStamp,
                    Distance_km = Math.Round(d, 2),
                    Bearing_deg = Math.Round(b, 0),
                    Closing_ms = Math.Round(cr, 1),
                    TTCA_s = double.IsInfinity(ttca) ? double.PositiveInfinity : Math.Round(ttca, 1),
                    CPA_Dist_km = Math.Round(cpaD, 2),
                    TCPA_s = double.IsInfinity(tcpa) ? double.PositiveInfinity : Math.Round(tcpa, 1),
                    X_km = c.X_km,
                    Y_km = c.Y_km,
                    Vx_kms = c.Vx_kms,
                    Vy_kms = c.Vy_kms
                });

                //  latched, bis Ack
                if (cpaD < 1.0)
                    _alarms.RaiseOrUpdate($"Proximity.Contact#{c.Id}", Severity.Alarm,
                        $"CPA < 1 km in {FormatS(tcpa)} (aktuell {d:F1} km, Bearing {b:F0}°, closing {cr:F0} m/s)",
                        d, latched: true);
                else if (d < 2.0)
                    _alarms.RaiseOrUpdate($"Proximity.Contact#{c.Id}", Severity.Alarm,
                        $"Distanz < 2 km (Bearing {b:F0}°, closing {cr:F0} m/s, TCPA {FormatS(tcpa)})",
                        d, latched: true);
                else if (cpaD < 3.0)
                    _alarms.RaiseOrUpdate($"Proximity.Contact#{c.Id}", Severity.Warning,
                        $"CPA < 3 km in {FormatS(tcpa)} (aktuell {d:F1} km, Bearing {b:F0}°)",
                        d, latched: true);
                else if (d < 5.0)
                    _alarms.RaiseOrUpdate($"Proximity.Contact#{c.Id}", Severity.Warning,
                        $"Distanz < 5 km (Bearing {b:F0}°, closing {cr:F0} m/s)",
                        d, latched: true);
            }

            OnPropertyChanged(nameof(HighestSeverity));
        }, null);
    }

    private static string FormatS(double s) =>
        double.IsInfinity(s) ? "∞" : $"{Math.Max(0, Math.Round(s))} s";

    private readonly SynchronizationContext _ui;

}
