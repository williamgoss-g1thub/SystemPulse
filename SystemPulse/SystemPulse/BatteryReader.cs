using System.Management;

namespace SystemPulse
{
    public class BatteryReader
    {
        public List<(string SensorType, float Value)> ReadAll()
        {
            var results = new List<(string, float)>();

            try
            {
                float? designCapacity = null;
                float? fullChargeCapacity = null;
                float? cycleCount = null;
                float? chargePercent = null;

                using (var searcher = new ManagementObjectSearcher(
                    @"root\WMI", "SELECT * FROM BatteryStaticData"))
                {
                    foreach (var obj in searcher.Get())
                        designCapacity = Convert.ToSingle(obj["DesignedCapacity"]);
                }

                using (var searcher = new ManagementObjectSearcher(
                    @"root\WMI", "SELECT * FROM BatteryFullChargedCapacity"))
                {
                    foreach (var obj in searcher.Get())
                        fullChargeCapacity = Convert.ToSingle(obj["FullChargedCapacity"]);
                }

                using (var searcher = new ManagementObjectSearcher(
                    @"root\WMI", "SELECT * FROM BatteryCycleCount"))
                {
                    foreach (var obj in searcher.Get())
                        cycleCount = Convert.ToSingle(obj["CycleCount"]);
                }

                using (var searcher = new ManagementObjectSearcher(
                    "SELECT * FROM Win32_Battery"))
                {
                    foreach (var obj in searcher.Get())
                        chargePercent = Convert.ToSingle(obj["EstimatedChargeRemaining"]);
                }

                if (designCapacity != null && fullChargeCapacity != null && designCapacity > 0)
                {
                    float healthPercent = (fullChargeCapacity.Value / designCapacity.Value) * 100f;
                    results.Add(("battery_health_pct", healthPercent));
                }

                if (cycleCount != null)
                    results.Add(("battery_cycle_count", cycleCount.Value));

                if (chargePercent != null)
                    results.Add(("battery_charge_pct", chargePercent.Value));
            }
            catch
            {
                // Some laptops don't expose all of these — fail silently
            }

            return results;
        }
    }
}