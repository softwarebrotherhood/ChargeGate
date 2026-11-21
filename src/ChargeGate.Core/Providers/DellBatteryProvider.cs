using System.Diagnostics;
using System.Management;
using ChargeGate.Core.Interfaces;
using ChargeGate.Core.Models;
using ChargeGate.Core.Services;

namespace ChargeGate.Core.Providers;

public class DellBatteryProvider : IBatteryProvider
{
    private const string WmiNamespace = "root\\dcim\\sysman";
    private const string WmiClassName = "DCIM_BatteryChargingSettings";

    public VendorType Vendor => VendorType.Dell;
    public string ProviderName => "Dell DCIM / Command WMI";

    public bool IsSupported { get; private set; }

    public IReadOnlyList<ThresholdOption> SupportedThresholds { get; } = new List<ThresholdOption>
    {
        new(80, "Primarily AC Use (80%)", "Lowers charging threshold to protect battery for users constantly plugged in"),
        new(85, "Adaptive Mode", "Dell automatically balances charging based on typical usage pattern"),
        new(100, "Standard (100%)", "Charges battery to full 100% capacity at standard rate")
    };

    public DellBatteryProvider()
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
        catch { return false; }
    }

    public Task<int> GetCurrentThresholdAsync() => Task.Run(() =>
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(WmiNamespace, $"SELECT ChargingMode, HighThreshold FROM {WmiClassName}");
            foreach (ManagementObject obj in searcher.Get())
            {
                if (obj["HighThreshold"] != null)
                {
                    int high = Convert.ToInt32(obj["HighThreshold"]);
                    if (high > 0) return high;
                }
                
                var mode = obj["ChargingMode"]?.ToString();
                if (mode?.Contains("AC", StringComparison.OrdinalIgnoreCase) == true)
                    return 80;
                if (mode?.Contains("Adaptive", StringComparison.OrdinalIgnoreCase) == true)
                    return 85;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DellBatteryProvider] Error reading threshold: {ex.Message}");
        }

        return 100;
    });

    public Task<bool> SetThresholdAsync(int percentage) => Task.Run(() =>
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(WmiNamespace, $"SELECT * FROM {WmiClassName}");
            foreach (ManagementObject obj in searcher.Get())
            {
                using var inParams = obj.GetMethodParameters("SetChargingMode");
                // 1 = Standard, 2 = Express, 3 = AC, 4 = Auto, 5 = Custom
                uint mode = percentage switch
                {
                    <= 80 => 3u, // Primarily AC Use
                    <= 90 => 4u, // Adaptive
                    _ => 1u      // Standard 100%
                };

                inParams["Mode"] = mode;
                using var outParams = obj.InvokeMethod("SetChargingMode", inParams, null);
                var ret = Convert.ToUInt32(outParams["ReturnValue"]);
                if (ret == 0) return true;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DellBatteryProvider] Error setting threshold: {ex.Message}");
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
