using System.Diagnostics;
using System.Management;
using ChargeGate.Core.Interfaces;
using ChargeGate.Core.Models;
using ChargeGate.Core.Services;

namespace ChargeGate.Core.Providers;

public class LenovoBatteryProvider : IBatteryProvider
{
    private const string WmiNamespace = "root\\WMI";
    private const string ClassBatteryMode = "Lenovo_BatteryMode";
    private const string ClassThinkPad = "IBMPC_SetBatteryChargeThreshold";

    public VendorType Vendor => VendorType.Lenovo;
    public string ProviderName => "Lenovo Conservation / ThinkPad WMI";

    public bool IsSupported { get; private set; }
    private readonly bool _isThinkPad;

    public IReadOnlyList<ThresholdOption> SupportedThresholds { get; }

    public LenovoBatteryProvider()
    {
        _isThinkPad = CheckThinkPadSupport();
        IsSupported = _isThinkPad || CheckIdeaPadSupport();

        if (_isThinkPad)
        {
            SupportedThresholds = new List<ThresholdOption>
            {
                new(50, "Conservation (50%)", "Optimized for desk-bound ThinkPad docks"),
                new(75, "High Health (75%)", "Stop charge at 75% (starts below 70%)"),
                new(80, "Balanced Health (80%)", "Standard recommendation for long battery cycle life"),
                new(100, "Full 100%", "Charge fully for extended travel")
            };
        }
        else
        {
            SupportedThresholds = new List<ThresholdOption>
            {
                new(60, "Conservation Mode (60-80%)", "Prevents battery wearing down while connected to power adapter"),
                new(100, "Normal Charging (100%)", "Charges battery to 100% capacity")
            };
        }
    }

    private bool CheckIdeaPadSupport()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(WmiNamespace, $"SELECT * FROM {ClassBatteryMode}");
            using var results = searcher.Get();
            return results.Count > 0;
        }
        catch { return false; }
    }

    private bool CheckThinkPadSupport()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(WmiNamespace, $"SELECT * FROM {ClassThinkPad}");
            using var results = searcher.Get();
            return results.Count > 0;
        }
        catch { return false; }
    }

    public Task<int> GetCurrentThresholdAsync() => Task.Run(() =>
    {
        try
        {
            if (_isThinkPad)
            {
                using var searcher = new ManagementObjectSearcher(WmiNamespace, $"SELECT StopPercentage FROM {ClassThinkPad}");
                foreach (ManagementObject obj in searcher.Get())
                {
                    if (obj["StopPercentage"] != null)
                        return Convert.ToInt32(obj["StopPercentage"]);
                }
            }
            else
            {
                using var searcher = new ManagementObjectSearcher(WmiNamespace, $"SELECT ConservationMode FROM {ClassBatteryMode}");
                foreach (ManagementObject obj in searcher.Get())
                {
                    if (obj["ConservationMode"] != null)
                    {
                        bool active = Convert.ToBoolean(obj["ConservationMode"]);
                        return active ? 60 : 100;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[LenovoBatteryProvider] Error reading threshold: {ex.Message}");
        }

        return 100;
    });

    public Task<bool> SetThresholdAsync(int percentage) => Task.Run(() =>
    {
        try
        {
            if (_isThinkPad)
            {
                using var searcher = new ManagementObjectSearcher(WmiNamespace, $"SELECT * FROM {ClassThinkPad}");
                foreach (ManagementObject obj in searcher.Get())
                {
                    using var inParams = obj.GetMethodParameters("SetStopPercentage");
                    inParams["Percentage"] = (uint)percentage;
                    obj.InvokeMethod("SetStopPercentage", inParams, null);
                    return true;
                }
            }
            else
            {
                bool enableConservation = percentage <= 80;
                using var searcher = new ManagementObjectSearcher(WmiNamespace, $"SELECT * FROM {ClassBatteryMode}");
                foreach (ManagementObject obj in searcher.Get())
                {
                    using var inParams = obj.GetMethodParameters("SetConservationMode");
                    inParams["Mode"] = enableConservation ? 1u : 0u;
                    obj.InvokeMethod("SetConservationMode", inParams, null);
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[LenovoBatteryProvider] Error setting threshold: {ex.Message}");
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
