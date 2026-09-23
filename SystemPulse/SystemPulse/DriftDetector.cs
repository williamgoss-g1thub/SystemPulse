using Microsoft.Data.Sqlite;

namespace SystemPulse
{
    public class DriftAlert
    {
        public string SensorType { get; set; } = "";
        public double BaselineValue { get; set; }
        public double CurrentValue { get; set; }
        public double DeltaPercent { get; set; }
    }

    public static class DriftDetector
    {
        public static List<DriftAlert> CheckForDrift()
        {
            var alerts = new List<DriftAlert>();

            using var connection = new SqliteConnection(Database.ConnectionString);
            connection.Open();

            CheckBatteryHealth(connection, alerts);
            CheckGpuIdleTemp(connection, alerts);

            return alerts;
        }

        private static void CheckBatteryHealth(SqliteConnection connection, List<DriftAlert> alerts)
        {
            double? latest = GetLatestValue(connection, "battery_health_pct");
            double? earliest = GetEarliestValue(connection, "battery_health_pct");

            if (latest == null || earliest == null) return;

            double delta = earliest.Value - latest.Value;
            if (delta >= 2.0)
            {
                alerts.Add(new DriftAlert
                {
                    SensorType = "battery_health_pct",
                    BaselineValue = earliest.Value,
                    CurrentValue = latest.Value,
                    DeltaPercent = delta
                });
            }
        }

        private static void CheckGpuIdleTemp(SqliteConnection connection, List<DriftAlert> alerts)
        {
            double? rollingAvg = GetRollingIdleAverage(connection, "gpu_temp", daysBack: 14);
            double? todayAvg = GetTodayIdleAverage(connection, "gpu_temp");

            if (rollingAvg == null || todayAvg == null) return;

            double delta = todayAvg.Value - rollingAvg.Value;
            if (delta >= 5.0)
            {
                alerts.Add(new DriftAlert
                {
                    SensorType = "gpu_temp",
                    BaselineValue = rollingAvg.Value,
                    CurrentValue = todayAvg.Value,
                    DeltaPercent = delta
                });
            }
        }

        private static double? GetLatestValue(SqliteConnection connection, string sensorType)
        {
            var command = connection.CreateCommand();
            command.CommandText = "SELECT Value FROM Readings WHERE SensorType = $type ORDER BY Id DESC LIMIT 1;";
            command.Parameters.AddWithValue("$type", sensorType);
            var result = command.ExecuteScalar();
            return result == null ? null : Convert.ToDouble(result);
        }

        private static double? GetEarliestValue(SqliteConnection connection, string sensorType)
        {
            var command = connection.CreateCommand();
            command.CommandText = "SELECT Value FROM Readings WHERE SensorType = $type ORDER BY Id ASC LIMIT 1;";
            command.Parameters.AddWithValue("$type", sensorType);
            var result = command.ExecuteScalar();
            return result == null ? null : Convert.ToDouble(result);
        }

        private static double? GetRollingIdleAverage(SqliteConnection connection, string sensorType, int daysBack)
        {
            var command = connection.CreateCommand();
            command.CommandText =
            @"
                SELECT AVG(Value) FROM Readings
                WHERE SensorType = $type
                  AND SystemState = 'idle'
                  AND Timestamp >= $cutoff;
            ";
            command.Parameters.AddWithValue("$type", sensorType);
            command.Parameters.AddWithValue("$cutoff", DateTime.UtcNow.AddDays(-daysBack).ToString("o"));
            var result = command.ExecuteScalar();
            return result == DBNull.Value || result == null ? null : Convert.ToDouble(result);
        }

        private static double? GetTodayIdleAverage(SqliteConnection connection, string sensorType)
        {
            var command = connection.CreateCommand();
            command.CommandText =
            @"
                SELECT AVG(Value) FROM Readings
                WHERE SensorType = $type
                  AND SystemState = 'idle'
                  AND Timestamp >= $cutoff;
            ";
            command.Parameters.AddWithValue("$type", sensorType);
            command.Parameters.AddWithValue("$cutoff", DateTime.UtcNow.Date.ToString("o"));
            var result = command.ExecuteScalar();
            return result == DBNull.Value || result == null ? null : Convert.ToDouble(result);
        }
    }
}