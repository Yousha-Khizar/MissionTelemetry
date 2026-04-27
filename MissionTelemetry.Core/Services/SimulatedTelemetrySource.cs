using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MissionTelemetry.Core.Models;
using System.Timers;

namespace MissionTelemetry.Core.Services
{
    public sealed class SimulatedTelemetrySource : ITelemtrySource
    {
        private readonly System.Timers.Timer _timer;   // periodische updates
        private long _seq;
        private readonly Random _rnd = new();

        private double _voltage = 28.5, _current = 2.4, _boardTemp = 28.0, _snr = 18.0;
        private double _roll, _pitch, _yaw;

        public event EventHandler<TelemetryFrame>? FrameReceived;
        public bool IsRunning => _timer.Enabled;

        public SimulatedTelemetrySource(double hz = 1.0)                 // Event quelle für frames
        {
            _timer = new System.Timers.Timer(1000.0 / hz) { AutoReset = true };

            _timer.Elapsed += (_, __) => Tick();


        }
        
        public void Start() => _timer.Start();
        public void Stop() => _timer.Stop();    


        private void Tick()
        {
            _seq++;
            var r = _rnd;

            _voltage += (r.NextDouble() - 0.5) * 0.02;
            _current += (r.NextDouble() - 0.5) * 0.05;
            _boardTemp += (r.NextDouble() - 0.5) * 0.1;
            _snr += (r.NextDouble() - 0.5) * 0.5;
            _roll += (r.NextDouble() - 0.5) * 0.3;
            _pitch += (r.NextDouble() - 0.5) * 0.3;
            _yaw += (r.NextDouble() - 0.5) * 0.3;

            if (r.NextDouble() < 0.02) _snr -= 5;
            if(r.NextDouble() < 0.01) _boardTemp += 3;

            var values = new Dictionary<string, double>
            {
                ["Power.Voltage"] = Math.Round(_voltage, 3),
                ["Power.Current"] = Math.Round(_current, 3),
                ["Thermal.BoardTemp"] = Math.Round(_boardTemp, 2),
                ["Comms.SNR"] = Math.Round(_snr, 2),
                ["Attitude.Roll"] = Math.Round(_roll, 2),
                ["Attitude.Pitch"] = Math.Round(_pitch, 2),
                ["Attitude.Yaw"] = Math.Round(_yaw, 2),

            };

            FrameReceived?.Invoke
                (
                this, new TelemetryFrame
                {
                    Sequence = _seq,
                    TimeStamp = DateTime.Now,
                    Values = values
                }
                );

        }

        public void Dispose()
        { _timer.Stop(); _timer.Dispose(); }

    }
}
