using System.Diagnostics;
using System.Management;
using ChargeGate.Core.Interfaces;
using ChargeGate.Core.Models;
using ChargeGate.Core.Services;

namespace ChargeGate.Core.Providers;

public class LgBatteryProvider : IBatteryProvider
{
    private const string WmiNamespace = "root\\WMI";
    private const string WmiClassName = "LG_BatteryManagement";

    public VendorType Vendor => VendorType.LG;
    public string ProviderName => "LG Gram Control Center";

    public bool IsSupported { get; private set; }

    public IReadOnlyList<ThresholdOption> SupportedThresholds { get; } = new List<ThresholdOption>
    {
        new(80, "Battery Care (80%)", "Caps charge at 80% to maximize LG Gram battery life"),
        new(100, "Full 100% Charge", "Standard maximum capacity charging")
    };

    public LgBatteryProvider()
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
                if (obj["BatteryCareMode"] != null)
                {
                    bool careActive = Convert.ToBoolean(obj["BatteryCareMode"]);
                    return careActive ? 80 : 100;
                }
            }
        }
        catch { }

        return 80;
    });

    public Task<bool> SetThresholdAsync(int percentage) => Task.Run(() =>
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(WmiNamespace, $"SELECT * FROM {WmiClassName}");
            foreach (ManagementObject obj in searcher.Get())
            {
                using var inParams = obj.GetMethodParameters("SetBatteryCare");
                inParams["Enable"] = percentage <= 80;
                obj.InvokeMethod("SetBatteryCare", inParams, null);
                return true;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[LgBatteryProvider] Set error: {ex.Message}");
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
