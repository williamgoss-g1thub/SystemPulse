using System.Diagnostics;
using System.Timers;
using Microsoft.Data.Sqlite;
using Timer = System.Timers.Timer;

namespace SystemPulse
{
    public class LoggingService
    {
        private readonly SensorReader _gpuReader;
        private readonly BatteryReader _batteryReader;
        private readonly Timer _timer;
        private readonly PerformanceCounter _cpuCounter;

        public LoggingService()
        {
            _gpuReader = new SensorReader();
            _batteryReader = new BatteryReader();
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");

            _timer = new Timer(60000);
            _timer.Elapsed += OnTimerElapsed;
            _timer.AutoReset = true;
        }

        public void Start()
        {
            LogReadings();
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
            _gpuReader.Close();
        }

        private void OnTimerElapsed(object? sender, ElapsedEventArgs e) => LogReadings();

        private void LogReadings()
        {
            string state = GetSystemState();
            string timestamp = DateTime.UtcNow.ToString("o");

            var readings = new List<(string SensorType, float Value)>();
            readings.AddRange(_gpuReader.ReadAll());
            readings.AddRange(_batteryReader.ReadAll());

            using var connection = new SqliteConnection(Database.ConnectionString);
            connection.Open();

            foreach (var (sensorType, value) in readings)
            {
                var command = connection.CreateCommand();
                command.CommandText =
                @"
                    INSERT INTO Readings (Timestamp, SensorType, Value, SystemState)
                    VALUES ($timestamp, $sensorType, $value, $state);
                ";
                command.Parameters.AddWithValue("$timestamp", timestamp);
                command.Parameters.AddWithValue("$sensorType", sensorType);
                command.Parameters.AddWithValue("$value", value);
                command.Parameters.AddWithValue("$state", state);
                command.ExecuteNonQuery();
            }
        }

        private string GetSystemState()
        {
            float usage = _cpuCounter.NextValue();
            System.Threading.Thread.Sleep(200);
            usage = _cpuCounter.NextValue();
            return usage < 20 ? "idle" : "load";
        }
    }
}