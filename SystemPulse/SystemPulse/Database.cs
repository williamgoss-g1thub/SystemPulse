using Microsoft.Data.Sqlite;

namespace SystemPulse
{
    public static class Database
    {
        private static string DbPath =>
            System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SystemPulse", "systempulse.db");

        public static string ConnectionString => $"Data Source={DbPath}";

        public static void Initialize()
        {
            var dir = System.IO.Path.GetDirectoryName(DbPath)!;
            System.IO.Directory.CreateDirectory(dir);

            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText =
            @"
                CREATE TABLE IF NOT EXISTS Readings (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Timestamp TEXT NOT NULL,
                    SensorType TEXT NOT NULL,
                    Value REAL NOT NULL,
                    SystemState TEXT NOT NULL
                );
            ";
            command.ExecuteNonQuery();
        }
    }
}