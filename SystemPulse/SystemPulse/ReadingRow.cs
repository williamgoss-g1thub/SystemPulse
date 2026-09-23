namespace SystemPulse
{
    public class ReadingRow
    {
        public string Timestamp { get; set; } = "";
        public string SensorType { get; set; } = "";
        public double Value { get; set; }
        public string SystemState { get; set; } = "";
    }
}