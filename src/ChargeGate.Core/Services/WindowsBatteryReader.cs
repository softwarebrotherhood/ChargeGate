using System.Management;
using ChargeGate.Core.Models;

namespace ChargeGate.Core.Services;

public static class WindowsBatteryReader
{
    private static int _cachedDesignCapacity = 0;
    private static int _cachedCycleCount = -1;

    public static BatterySnapshot ReadSnapshot(int currentThreshold = 100, string activeModeLabel = "")
    {
        int chargePercent = 100;
        bool isPluggedIn = true;
        bool isCharging = false;
        bool isDischarging = false;
        int remainingMinutes = -1;
        int designCapacity = _cachedDesignCapacity;
        int fullChargeCapacity = 0;
        double voltageVolts = 0;
        double rateWatts = 0;
        int cycleCount = _cachedCycleCount;

        try
        {
            using var searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT EstimatedChargeRemaining, BatteryStatus, EstimatedRunTime FROM Win32_Battery");
            foreach (ManagementObject obj in searcher.Get())
            {
                if (obj["EstimatedChargeRemaining"] != null)
                    chargePercent = Convert.ToInt32(obj["EstimatedChargeRemaining"]);
                
                if (obj["EstimatedRunTime"] != null)
                    remainingMinutes = Convert.ToInt32(obj["EstimatedRunTime"]);

                if (obj["BatteryStatus"] != null)
                {
                    int status = Convert.ToInt32(obj["BatteryStatus"]);
                    // 1 = Other, 2 = Unknown, 3 = Fully Charged, 4 = Low, 5 = Critical, 6 = Charging, 7 = Charging and High, etc.
                    isCharging = status is 6 or 7 or 8 or 9;
                    isDischarging = status == 1;
                }
                break;
            }
        }
        catch
        {
            // Fallback or ignore
        }

        try
        {
            using var searcherWmi = new ManagementObjectSearcher("root\\wmi", "SELECT PowerOnline, Charging, Discharging, RemainingCapacity, Voltage, DischargeRate, ChargeRate FROM BatteryStatus");
            foreach (ManagementObject obj in searcherWmi.Get())
            {
                if (obj["PowerOnline"] != null)
                    isPluggedIn = Convert.ToBoolean(obj["PowerOnline"]);
                if (obj["Charging"] != null)
                    isCharging = Convert.ToBoolean(obj["Charging"]);
                if (obj["Discharging"] != null)
                    isDischarging = Convert.ToBoolean(obj["Discharging"]);

                if (obj["Voltage"] != null)
                {
                    double mv = Convert.ToDouble(obj["Voltage"]);
                    if (mv > 0) voltageVolts = Math.Round(mv / 1000.0, 2);
                }

                if (obj["DischargeRate"] != null)
                {
                    double dr = Convert.ToDouble(obj["DischargeRate"]);
                    if (dr > 0) rateWatts = Math.Round(-dr / 1000.0, 1);
                }
                else if (obj["ChargeRate"] != null)
                {
                    double cr = Convert.ToDouble(obj["ChargeRate"]);
                    if (cr > 0) rateWatts = Math.Round(cr / 1000.0, 1);
                }
                break;
            }
        }
        catch
        {
            // Fallback or ignore
        }

        try
        {
            using var searcherFull = new ManagementObjectSearcher("root\\wmi", "SELECT FullChargedCapacity FROM BatteryFullChargedCapacity");
            foreach (ManagementObject obj in searcherFull.Get())
            {
                if (obj["FullChargedCapacity"] != null)
                    fullChargeCapacity = Convert.ToInt32(obj["FullChargedCapacity"]);
                break;
            }
        }
        catch { }

        if (designCapacity == 0)
        {
            try
            {
                using var searcherDesign = new ManagementObjectSearcher("root\\CIMV2", "SELECT DesignCapacity FROM Win32_PortableBattery");
                foreach (ManagementObject obj in searcherDesign.Get())
                {
                    if (obj["DesignCapacity"] != null)
                        designCapacity = Convert.ToInt32(obj["DesignCapacity"]);
                    break;
                }
            }
            catch { }

            // If design capacity is missing from Win32_PortableBattery, try root\wmi BatteryStaticData
            if (designCapacity == 0)
            {
                try
                {
                    using var searcherStatic = new ManagementObjectSearcher("root\\wmi", "SELECT DesignedCapacity FROM BatteryStaticData");
                    foreach (ManagementObject obj in searcherStatic.Get())
                    {
                        if (obj["DesignedCapacity"] != null)
                            designCapacity = Convert.ToInt32(obj["DesignedCapacity"]);
                        break;
                    }
                }
                catch { }
            }

            if (designCapacity > 0)
            {
                _cachedDesignCapacity = designCapacity;
            }
        }

        if (_cachedCycleCount < 0)
        {
            try
            {
                using var searcherCycle = new ManagementObjectSearcher("root\\wmi", "SELECT CycleCount FROM BatteryCycleCount");
                foreach (ManagementObject obj in searcherCycle.Get())
                {
                    if (obj["CycleCount"] != null)
                    {
                        cycleCount = Convert.ToInt32(obj["CycleCount"]);
                        _cachedCycleCount = cycleCount;
                        break;
                    }
                }
            }
            catch { }
        }

        return new BatterySnapshot
        {
            ChargePercentage = chargePercent,
            IsPluggedIn = isPluggedIn,
            IsCharging = isCharging,
            IsDischarging = isDischarging,
            EstimatedRemainingMinutes = remainingMinutes,
            DesignCapacityMWh = designCapacity,
            FullChargeCapacityMWh = fullChargeCapacity,
            CycleCount = cycleCount,
            VoltageVolts = voltageVolts,
            RateWatts = rateWatts,
            CurrentThresholdPercentage = currentThreshold,
            ActiveModeLabel = activeModeLabel,
            Timestamp = DateTime.UtcNow
        };
    }
}
