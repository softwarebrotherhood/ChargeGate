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
    public event Action<bool>? TravelModeStateChanged;

    public bool IsTravelModeActive { get; private set; }
    public int TravelRestoreThreshold { get; private set; } = 80;
    public BatteryTelemetryService TelemetryService { get; } = new();

    private bool _alertFiredForCurrentCycle = false;

    public BatteryManager()
    {
        DetectHardware();
        ActiveProvider = ResolveProvider();
        CurrentSnapshot = WindowsBatteryReader.ReadSnapshot();
        TelemetryService.RecordSnapshot(CurrentSnapshot);
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

        if (mfg.Contains("ACER"))
        {
            var acer = new AcerBatteryProvider();
            if (acer.IsSupported) return acer;
        }

        if (mfg.Contains("SURFACE") || MachineModel.ToUpperInvariant().Contains("SURFACE") || mfg.Contains("MICROSOFT"))
        {
            var surface = new SurfaceBatteryProvider();
            if (surface.IsSupported) return surface;
        }

        if (mfg.Contains("LG") || mfg.Contains("LG ELECTRONICS"))
        {
            var lg = new LgBatteryProvider();
            if (lg.IsSupported) return lg;
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
        TelemetryService.RecordSnapshot(snapshot);
        SnapshotUpdated?.Invoke(snapshot);

        // Check for Travel Mode auto-revert upon unplugging AC
        if (IsTravelModeActive && !snapshot.IsPluggedIn)
        {
            IsTravelModeActive = false;
            TravelModeStateChanged?.Invoke(false);
            int restoreTarget = TravelRestoreThreshold;
            _ = Task.Run(async () =>
            {
                await SetThresholdAsync(restoreTarget);
                AlertTriggered?.Invoke($"Travel Mode complete: Charger disconnected. Restored {restoreTarget}% battery health limit.");
            });
        }

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

    public async Task<bool> ActivateTravelModeAsync()
    {
        if (CurrentSnapshot.CurrentThresholdPercentage < 100)
        {
            TravelRestoreThreshold = CurrentSnapshot.CurrentThresholdPercentage;
        }
        else
        {
            TravelRestoreThreshold = 80;
        }

        bool success = await SetThresholdAsync(100);
        if (success)
        {
            IsTravelModeActive = true;
            TravelModeStateChanged?.Invoke(true);
        }
        return success;
    }

    public async Task<bool> CancelTravelModeAsync()
    {
        bool success = await SetThresholdAsync(TravelRestoreThreshold);
        if (success)
        {
            IsTravelModeActive = false;
            TravelModeStateChanged?.Invoke(false);
        }
        return success;
    }

    public async Task<bool> SetThresholdAsync(int percentage)
    {
        if (percentage < 100 && IsTravelModeActive)
        {
            IsTravelModeActive = false;
            TravelModeStateChanged?.Invoke(false);
        }

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
