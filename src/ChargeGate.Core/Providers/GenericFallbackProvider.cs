using ChargeGate.Core.Interfaces;
using ChargeGate.Core.Models;
using ChargeGate.Core.Services;

namespace ChargeGate.Core.Providers;

public class GenericFallbackProvider : IBatteryProvider
{
    public VendorType Vendor => VendorType.Generic;
    public string ProviderName => "Universal Smart Alert Monitor";
    public bool IsSupported => true;

    private int _threshold = 80;

    public IReadOnlyList<ThresholdOption> SupportedThresholds { get; } = new List<ThresholdOption>
    {
        new(70, "Alert at 70%", "Notifies you when battery hits 70% to disconnect charger"),
        new(80, "Alert at 80%", "Standard 80% golden rule notification"),
        new(90, "Alert at 90%", "Notifies you at 90% before high-voltage strain occurs"),
        new(100, "Full 100% Alert", "Notifies when battery reaches full charge")
    };

    public Task<int> GetCurrentThresholdAsync() => Task.FromResult(_threshold);

    public Task<bool> SetThresholdAsync(int percentage)
    {
        _threshold = Math.Clamp(percentage, 50, 100);
        return Task.FromResult(true);
    }

    public Task<BatterySnapshot> GetSnapshotAsync()
    {
        string activeLabel = SupportedThresholds.FirstOrDefault(t => t.Percentage == _threshold)?.Label ?? $"Alert at {_threshold}%";
        return Task.FromResult(WindowsBatteryReader.ReadSnapshot(_threshold, activeLabel));
    }
}
