using System.Management;
using ChargeGate.Core.Models;

namespace ChargeGate.Core.Services;

public static class WindowsBatteryReader
{
    private static int _cachedDesignCapacity = 0;

    public static BatterySnapshot ReadSnapshot(int currentThreshold = 100, string activeModeLabel = "")
    {
        int chargePercent = 100;
        bool isPluggedIn = true;
        bool isCharging = false;
        bool isDischarging = false;
        int remainingMinutes = -1;
        int designCapacity = _cachedDesignCapacity;
        int fullChargeCapacity = 0;

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
            using var searcherWmi = new ManagementObjectSearcher("root\\wmi", "SELECT PowerOnline, Charging, Discharging, RemainingCapacity FROM BatteryStatus");
            foreach (ManagementObject obj in searcherWmi.Get())
            {
                if (obj["PowerOnline"] != null)
                    isPluggedIn = Convert.ToBoolean(obj["PowerOnline"]);
                if (obj["Charging"] != null)
                    isCharging = Convert.ToBoolean(obj["Charging"]);
                if (obj["Discharging"] != null)
                    isDischarging = Convert.ToBoolean(obj["Discharging"]);
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

        return new BatterySnapshot
        {
            ChargePercentage = chargePercent,
            IsPluggedIn = isPluggedIn,
            IsCharging = isCharging,
            IsDischarging = isDischarging,
            EstimatedRemainingMinutes = remainingMinutes,
            DesignCapacityMWh = designCapacity,
            FullChargeCapacityMWh = fullChargeCapacity,
            CurrentThresholdPercentage = currentThreshold,
            ActiveModeLabel = activeModeLabel,
            Timestamp = DateTime.UtcNow
        };
    }
}
