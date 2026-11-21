using System.Diagnostics;
using System.Management;
using ChargeGate.Core.Interfaces;
using ChargeGate.Core.Models;
using ChargeGate.Core.Providers;

namespace ChargeGate.Core.Services;

public class BatteryManager : IDisposable
{
    private readonly PeriodicTimer _timer = new(TimeSpan.FromSeconds(4));
    private CancellationTokenSource _cts = new();
    private Task? _monitorTask;

    public IBatteryProvider ActiveProvider { get; private set; }
    public BatterySnapshot CurrentSnapshot { get; private set; }
    public bool IsHardwareControlled => ActiveProvider.Vendor != VendorType.Generic;
    public string MachineManufacturer { get; private set; } = "Unknown";
    public string MachineModel { get; private set; } = "PC";

    public event Action<BatterySnapshot>? SnapshotUpdated;
    public event Action<int, string>? ThresholdChanged;
    public event Action<string>? AlertTriggered;

    private bool _alertFiredForCurrentCycle = false;

    public BatteryManager()
    {
        DetectHardware();
        ActiveProvider = ResolveProvider();
        CurrentSnapshot = WindowsBatteryReader.ReadSnapshot();
    }

    private void DetectHardware()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT Manufacturer, Model FROM Win32_ComputerSystem");
            foreach (ManagementObject obj in searcher.Get())
            {
                MachineManufacturer = obj["Manufacturer"]?.ToString() ?? "Unknown";
                MachineModel = obj["Model"]?.ToString() ?? "PC";
                break;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[BatteryManager] Detection error: {ex.Message}");
        }
    }

    private IBatteryProvider ResolveProvider()
    {
        string mfg = MachineManufacturer.ToUpperInvariant();

        if (mfg.Contains("HP") || mfg.Contains("HEWLETT"))
        {
            var hp = new HpBatteryProvider();
            if (hp.IsSupported) return hp;
        }

        if (mfg.Contains("ASUS") || mfg.Contains("ASUSTEK"))
        {
            var asus = new AsusBatteryProvider();
            if (asus.IsSupported) return asus;
        }

        if (mfg.Contains("LENOVO"))
        {
            var lenovo = new LenovoBatteryProvider();
            if (lenovo.IsSupported) return lenovo;
        }

        if (mfg.Contains("DELL"))
        {
            var dell = new DellBatteryProvider();
            if (dell.IsSupported) return dell;
        }

        // Generic fallback if no specific hardware provider matches or is supported
        return new GenericFallbackProvider();
    }

    public void StartMonitoring()
    {
        if (_monitorTask != null) return;
        _cts = new CancellationTokenSource();
        _monitorTask = MonitorLoopAsync(_cts.Token);
    }

    public void StopMonitoring()
    {
        _cts.Cancel();
        _monitorTask = null;
    }

    private async Task MonitorLoopAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested && await _timer.WaitForNextTickAsync(token))
            {
                await RefreshSnapshotAsync();
            }
        }
        catch (OperationCanceledException) { }
    }

    public async Task<BatterySnapshot> RefreshSnapshotAsync()
    {
        var snapshot = await ActiveProvider.GetSnapshotAsync();
        CurrentSnapshot = snapshot;
        SnapshotUpdated?.Invoke(snapshot);

        // Check for alert conditions in fallback mode
        if (!IsHardwareControlled && snapshot.IsPluggedIn && snapshot.ChargePercentage >= snapshot.CurrentThresholdPercentage)
        {
            if (!_alertFiredForCurrentCycle)
            {
                _alertFiredForCurrentCycle = true;
                AlertTriggered?.Invoke($"Battery reached {snapshot.ChargePercentage}%! Unplug charger to preserve lifespan.");
            }
        }
        else if (!snapshot.IsPluggedIn || snapshot.ChargePercentage < snapshot.CurrentThresholdPercentage - 5)
        {
            _alertFiredForCurrentCycle = false;
        }

        return snapshot;
    }

    public async Task<bool> SetThresholdAsync(int percentage)
    {
        bool success = await ActiveProvider.SetThresholdAsync(percentage).ConfigureAwait(false);
        if (success)
        {
            var opt = ActiveProvider.SupportedThresholds.FirstOrDefault(t => t.Percentage == percentage);
            string modeLabel = opt?.Label ?? $"{percentage}%";

            CurrentSnapshot = CurrentSnapshot with
            {
                CurrentThresholdPercentage = percentage,
                ActiveModeLabel = modeLabel
            };

            ThresholdChanged?.Invoke(percentage, modeLabel);
            SnapshotUpdated?.Invoke(CurrentSnapshot);
        }
        return success;
    }

    public void Dispose()
    {
        StopMonitoring();
        _cts.Dispose();
        _timer.Dispose();
    }
}
