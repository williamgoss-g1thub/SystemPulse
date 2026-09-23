using LibreHardwareMonitor.Hardware;

namespace SystemPulse
{
    public class SensorReader
    {
        private readonly Computer _computer;

        public SensorReader()
        {
            _computer = new Computer
            {
                IsGpuEnabled = true
            };
            _computer.Open();
        }

        public List<(string SensorType, float Value)> ReadAll()
        {
            var results = new List<(string, float)>();

            foreach (var hardware in _computer.Hardware)
            {
                hardware.Update();

                foreach (var sensor in hardware.Sensors)
                {
                    if (sensor.Value == null) continue;

                    if (sensor.SensorType == SensorType.Temperature &&
                        sensor.Name == "GPU Core")
                    {
                        results.Add(("gpu_temp", sensor.Value.Value));
                    }
                }
            }

            return results;
        }

        public void Close() => _computer.Close();
    }
}