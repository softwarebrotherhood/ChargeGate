using System.Diagnostics;
using System.Management;
using ChargeGate.Core.Interfaces;
using ChargeGate.Core.Models;
using ChargeGate.Core.Services;

namespace ChargeGate.Core.Providers;

public class AcerBatteryProvider : IBatteryProvider
{
    private const string WmiNamespace = "root\\WMI";
    private const string WmiClassName = "Acer_BatteryChargeThreshold";

    public VendorType Vendor => VendorType.Acer;
    public string ProviderName => "Acer Care Center WMI";

    public bool IsSupported { get; private set; }

    public IReadOnlyList<ThresholdOption> SupportedThresholds { get; } = new List<ThresholdOption>
    {
        new(80, "Battery Health (80%)", "Caps charge at 80% to extend Acer battery longevity"),
        new(100, "Full Capacity (100%)", "Standard charging to 100% capacity")
    };

    public AcerBatteryProvider()
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
            using var searcher = new ManagementObjectSearcher(WmiNamespace, $"SELECT * FROM {WmiClassName}");
            foreach (ManagementObject obj in searcher.Get())
            {
                if (obj["Threshold"] != null)
                {
                    int val = Convert.ToInt32(obj["Threshold"]);
                    return val > 0 ? val : 80;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AcerBatteryProvider] Error reading threshold: {ex.Message}");
        }

        return 80;
    });

    public Task<bool> SetThresholdAsync(int percentage) => Task.Run(() =>
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(WmiNamespace, $"SELECT * FROM {WmiClassName}");
            foreach (ManagementObject obj in searcher.Get())
            {
                using var inParams = obj.GetMethodParameters("SetThreshold");
                inParams["Threshold"] = (uint)percentage;
                obj.InvokeMethod("SetThreshold", inParams, null);
                return true;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AcerBatteryProvider] Error setting threshold: {ex.Message}");
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
