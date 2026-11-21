using System.Diagnostics;
using System.Management;
using ChargeGate.Core.Interfaces;
using ChargeGate.Core.Models;
using ChargeGate.Core.Services;

namespace ChargeGate.Core.Providers;

public class AsusBatteryProvider : IBatteryProvider
{
    private const string WmiNamespace = "root\\wmi";
    private const string WmiClassName = "AsusAtkWmi_WMNB";
    private const uint DEVID_BATTERY_CHARGE_LIMIT = 0x00120057;

    public VendorType Vendor => VendorType.Asus;
    public string ProviderName => "ASUS ATK/WMI (G-Helper Compatible)";

    public bool IsSupported { get; private set; }

    public IReadOnlyList<ThresholdOption> SupportedThresholds { get; } = new List<ThresholdOption>
    {
        new(60, "Maximum Lifespan (60%)", "Best for laptops permanently plugged in as a desktop replacement"),
        new(80, "Balanced Health (80%)", "Recommended sweet spot for daily usage and battery preservation"),
        new(100, "Full Mobility (100%)", "Full 100% charge for long travel away from wall power")
    };

    public AsusBatteryProvider()
    {
        IsSupported = CheckSupport();
    }

    private bool CheckSupport()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(WmiNamespace, $"SELECT * FROM {WmiClassName}");
            using var results = searcher.Get();
            return results.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    public Task<int> GetCurrentThresholdAsync() => Task.Run(() =>
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(WmiNamespace, $"SELECT * FROM {WmiClassName}");
            foreach (ManagementObject obj in searcher.Get())
            {
                using var inParams = obj.GetMethodParameters("DEVS");
                inParams["Device_Arg"] = DEVID_BATTERY_CHARGE_LIMIT;
                using var outParams = obj.InvokeMethod("DEVS", inParams, null);
                
                var result = Convert.ToUInt32(outParams["Result"]);
                int limit = (int)(result & 0xFF);
                if (limit is 60 or 80 or 100)
                    return limit;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AsusBatteryProvider] Error reading threshold: {ex.Message}");
        }

        return 80;
    });

    public Task<bool> SetThresholdAsync(int percentage) => Task.Run(() =>
    {
        int target = percentage switch
        {
            <= 60 => 60,
            <= 80 => 80,
            _ => 100
        };

        try
        {
            using var searcher = new ManagementObjectSearcher(WmiNamespace, $"SELECT * FROM {WmiClassName}");
            foreach (ManagementObject obj in searcher.Get())
            {
                using var inParams = obj.GetMethodParameters("DSTS");
                inParams["Device_Arg"] = DEVID_BATTERY_CHARGE_LIMIT;
                inParams["Control_Status"] = (uint)target;

                using var outParams = obj.InvokeMethod("DSTS", inParams, null);
                var result = Convert.ToUInt32(outParams["Result"]);
                if (result == 1)
                {
                    Debug.WriteLine($"[AsusBatteryProvider] Successfully set charge limit to {target}%");
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AsusBatteryProvider] Error setting threshold: {ex.Message}");
        }

        return false;
    });

    public async Task<BatterySnapshot> GetSnapshotAsync()
    {
        int threshold = await GetCurrentThresholdAsync().ConfigureAwait(false);
        string activeLabel = SupportedThresholds.FirstOrDefault(t => t.Percentage == threshold)?.Label ?? $"{threshold}%";
        return await Task.Run(() => WindowsBatteryReader.ReadSnapshot(threshold, activeLabel)).ConfigureAwait(false);
    }
}
