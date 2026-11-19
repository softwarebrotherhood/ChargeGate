using System.Diagnostics;
using System.Management;
using ChargeGate.Core.Interfaces;
using ChargeGate.Core.Models;
using ChargeGate.Core.Services;

namespace ChargeGate.Core.Providers;

public class HpBatteryProvider : IBatteryProvider
{
    private const string WmiNamespace = "root\\HP\\InstrumentedBIOS";
    private const string SettingName = "Battery Health Manager";

    public const string ValHealth = "Maximize my battery health";
    public const string ValAdaptive = "Let HP manage my battery charging";
    public const string ValDuration = "Maximize my battery duration";

    public VendorType Vendor => VendorType.HP;
    public string ProviderName => "HP Instrumented BIOS";

    public bool IsSupported { get; private set; }

    public IReadOnlyList<ThresholdOption> SupportedThresholds { get; } = new List<ThresholdOption>
    {
        new(80, "Battery Health (80%)", "Caps charge at 80% to maximize lithium battery lifespan", ValHealth),
        new(85, "Adaptive Smart Mode", "Dynamically optimizes charging based on daily usage", ValAdaptive),
        new(100, "Maximum Duration (100%)", "Charges to 100% for maximum travel and battery runtime", ValDuration)
    };

    public HpBatteryProvider()
    {
        IsSupported = CheckSupport();
    }

    private bool CheckSupport()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(WmiNamespace, $"SELECT CurrentValue FROM HP_BIOSEnumeration WHERE Name = '{SettingName}'");
            using var results = searcher.Get();
            return results.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    public Task<int> GetCurrentThresholdAsync()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(WmiNamespace, $"SELECT CurrentValue FROM HP_BIOSEnumeration WHERE Name = '{SettingName}'");
            foreach (ManagementObject obj in searcher.Get())
            {
                var val = obj["CurrentValue"]?.ToString();
                return Task.FromResult(MapValueToPercentage(val));
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[HpBatteryProvider] Error reading threshold: {ex.Message}");
        }

        return Task.FromResult(100);
    }

    public Task<bool> SetThresholdAsync(int percentage)
    {
        string targetValue = percentage switch
        {
            <= 80 => ValHealth,
            <= 90 => ValAdaptive,
            _ => ValDuration
        };

        try
        {
            using var managementClass = new ManagementClass(WmiNamespace, "HP_BIOSSettingInterface", null);
            using var inParams = managementClass.GetMethodParameters("SetBIOSSetting");
            
            inParams["Name"] = SettingName;
            inParams["Value"] = targetValue;
            inParams["Password"] = string.Empty;

            using var outParams = managementClass.InvokeMethod("SetBIOSSetting", inParams, null);
            var returnVal = Convert.ToUInt32(outParams["Return"]);

            // Return == 0 means SUCCESS
            if (returnVal == 0)
            {
                Debug.WriteLine($"[HpBatteryProvider] Successfully set '{SettingName}' to '{targetValue}'");
                return Task.FromResult(true);
            }

            Debug.WriteLine($"[HpBatteryProvider] SetBIOSSetting returned code {returnVal} (6 = Access Denied / Admin required)");
            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[HpBatteryProvider] Error setting threshold: {ex.Message}");
            return Task.FromResult(false);
        }
    }

    public async Task<BatterySnapshot> GetSnapshotAsync()
    {
        int threshold = await GetCurrentThresholdAsync();
        string activeLabel = SupportedThresholds.FirstOrDefault(t => t.Percentage == threshold)?.Label ?? $"{threshold}%";
        return WindowsBatteryReader.ReadSnapshot(threshold, activeLabel);
    }

    public static int MapValueToPercentage(string? rawValue) => rawValue switch
    {
        ValHealth => 80,
        ValAdaptive => 85,
        ValDuration => 100,
        _ => 100
    };
}
