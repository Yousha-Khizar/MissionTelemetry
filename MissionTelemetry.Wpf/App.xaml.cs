using System;
using System.IO;
using System.Windows;
using MissionTelemetry.Core.Services;  // JsonDictionaryLoader, DataDrivenAlarmEvaluator
using MissionTelemetry.Core.ViewModels;

namespace MissionTelemetry.Wpf
{
    public partial class App : Application
    {
        private void OnStartup(object sender, StartupEventArgs e)
        {
            // mission_dict.json aus dem Ausgabeverzeichnis laden
            var path = Path.Combine(AppContext.BaseDirectory, "mission_dict.json");
            var dict = new JsonDictionaryLoader().LoadFromFile(path);

            // Data-driven Evaluator erzeugen 
            IAlarmEvaluator evaluator = new DataDrivenAlarmEvaluator(dict);

            // Simulation + AlarmManager erzeugen
            ITelemtrySource telemetrySource = new SimulatedTelemetrySource(1.0);
            IProximitySource proximitySource = new SimulatedProximitySource(1.0);
            IAlarmManager alarmManager = new AlarmManager();

            //ViewModel 
            var vm = new TelemetryViewModel(
                telemetrySource,
                evaluator,
                proximitySource,
                alarmManager);

            // Fenster erzeugen und DataContext setzen
            var window = new MainWindow
            {
                DataContext = vm
            };

            window.Show();
        }
    }
}


