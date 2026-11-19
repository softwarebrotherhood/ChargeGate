using ChargeGate.Core.Models;

namespace ChargeGate.Core.Interfaces;

public interface IBatteryProvider
{
    VendorType Vendor { get; }
    string ProviderName { get; }
    bool IsSupported { get; }
    IReadOnlyList<ThresholdOption> SupportedThresholds { get; }

    Task<int> GetCurrentThresholdAsync();
    Task<bool> SetThresholdAsync(int percentage);
    Task<BatterySnapshot> GetSnapshotAsync();
}
