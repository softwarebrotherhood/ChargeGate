using System.Diagnostics;
using System.Management;
using ChargeGate.Core.Interfaces;
using ChargeGate.Core.Models;
using ChargeGate.Core.Services;

namespace ChargeGate.Core.Providers;

public class SurfaceBatteryProvider : IBatteryProvider
{
    private const string WmiNamespace = "root\\surface";
    private const string WmiClassName = "SurfaceBatteryLimit";

    public VendorType Vendor => VendorType.Microsoft;
    public string ProviderName => "Microsoft Surface UEFI";

    public bool IsSupported { get; private set; }

    public IReadOnlyList<ThresholdOption> SupportedThresholds { get; } = new List<ThresholdOption>
    {
        new(50, "Kiosk & Desk Limit (50%)", "Surface UEFI Battery Limit mode for docked devices"),
        new(80, "Battery Health (80%)", "Surface Smart Charging 80% protection limit"),
        new(100, "Full 100% Charge", "Standard maximum capacity charging")
    };

    public SurfaceBatteryProvider()
    {
        IsSupported = CheckSupport();
    }

    private bool CheckSupport()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT Model FROM Win32_ComputerSystem WHERE Model LIKE '%Surface%'");
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
                if (obj["LimitPercentage"] != null)
                {
                    int val = Convert.ToInt32(obj["LimitPercentage"]);
                    return val > 0 ? val : 80;
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
                using var inParams = obj.GetMethodParameters("SetLimit");
                inParams["Limit"] = (uint)percentage;
                obj.InvokeMethod("SetLimit", inParams, null);
                return true;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SurfaceBatteryProvider] Set error: {ex.Message}");
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
