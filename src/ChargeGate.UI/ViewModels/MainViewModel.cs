using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using ChargeGate.Core.Models;
using ChargeGate.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ChargeGate.UI.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly BatteryManager _batteryManager;

    [ObservableProperty]
    private int _batteryPercentage;

    [ObservableProperty]
    private int _healthPercentage = 100;

    [ObservableProperty]
    private string _healthColor = "#10B981";

    [ObservableProperty]
    private string _capacityStats = string.Empty;

    [ObservableProperty]
    private string _cycleCountText = "Reading...";

    [ObservableProperty]
    private string _voltageText = string.Empty;

    [ObservableProperty]
    private string _powerRateText = "AC Passthrough";

    [ObservableProperty]
    private bool _isTravelMode;

    [ObservableProperty]
    private string _statusDescription = "Detecting power state...";

    [ObservableProperty]
    private bool _isPluggedIn;

    [ObservableProperty]
    private bool _isCharging;

    [ObservableProperty]
    private int _selectedThreshold = 80;

    [ObservableProperty]
    private string _activeModeTitle = "Battery Health (80%)";

    [ObservableProperty]
    private string _hardwareInfo = string.Empty;

    [ObservableProperty]
    private string _statusFeedback = "Ready";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _startWithWindows;

    [ObservableProperty]
    private string _currentTheme = "System";

    [ObservableProperty]
    private string _graphMode = "Watts"; // "Watts" or "Percentage"

    [ObservableProperty]
    private string _currentPowerWatts = "0.0 W";

    [ObservableProperty]
    private string _averageDrainText = "--";

    [ObservableProperty]
    private string _peakChargeText = "--";

    [ObservableProperty]
    private string _graphModeLabel = "Power Flow (Watts)";

    [ObservableProperty]
    private List<BatteryTelemetryPoint> _telemetryHistory = new();

    private bool _isApplying;

    public ObservableCollection<ThresholdItemViewModel> ThresholdOptions { get; } = new();

    public MainViewModel()
    {
        _batteryManager = new BatteryManager();
        _batteryManager.SnapshotUpdated += OnSnapshotUpdated;
        _batteryManager.AlertTriggered += OnAlertTriggered;
        _batteryManager.TravelModeStateChanged += state => Application.Current?.Dispatcher?.Invoke(() => IsTravelMode = state);
        _batteryManager.TelemetryService.HistoryUpdated += OnHistoryUpdated;

        HardwareInfo = $"{_batteryManager.MachineManufacturer} {_batteryManager.MachineModel} • {_batteryManager.ActiveProvider.ProviderName}";
        _startWithWindows = AutoStartService.IsAutoStartEnabled();

        foreach (var opt in _batteryManager.ActiveProvider.SupportedThresholds)
        {
            ThresholdOptions.Add(new ThresholdItemViewModel(
                opt.Percentage,
                opt.Label,
                opt.Description,
                opt.RawVendorValue,
                opt.Percentage == SelectedThreshold));
        }

        OnHistoryUpdated();
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        _batteryManager.StartMonitoring();
        var snap = await Task.Run(() => _batteryManager.RefreshSnapshotAsync());
        UpdateFromSnapshot(snap);
    }

    private void OnSnapshotUpdated(BatterySnapshot snap)
    {
        Application.Current?.Dispatcher?.Invoke(() => UpdateFromSnapshot(snap));
    }

    private void UpdateFromSnapshot(BatterySnapshot snap)
    {
        BatteryPercentage = snap.ChargePercentage;
        HealthPercentage = snap.HealthPercentage;
        IsPluggedIn = snap.IsPluggedIn;
        IsCharging = snap.IsCharging;
        SelectedThreshold = snap.CurrentThresholdPercentage;
        ActiveModeTitle = snap.ActiveModeLabel;

        // Health Color
        HealthColor = HealthPercentage switch
        {
            >= 80 => "#10B981", // Green
            >= 60 => "#F59E0B", // Amber
            _ => "#EF4444"      // Red
        };

        // Capacity calculation
        if (snap.DesignCapacityMWh > 0 && snap.FullChargeCapacityMWh > 0)
        {
            double fullWh = Math.Round(snap.FullChargeCapacityMWh / 1000.0, 1);
            double designWh = Math.Round(snap.DesignCapacityMWh / 1000.0, 1);
            CapacityStats = $"{fullWh} Wh / {designWh} Wh";
        }
        else
        {
            CapacityStats = "Standard Pack";
        }

        CycleCountText = snap.CycleCount >= 0 ? $"{snap.CycleCount:N0} cycles" : "Smart Pack";
        VoltageText = snap.VoltageVolts > 0 ? $"{snap.VoltageVolts:F2} V" : string.Empty;

        if (snap.RateWatts != 0)
        {
            PowerRateText = snap.RateWatts > 0 ? $"+{snap.RateWatts:F1} W (Charging)" : $"{snap.RateWatts:F1} W (Discharging)";
        }
        else if (snap.IsPluggedIn && !snap.IsCharging)
        {
            PowerRateText = "AC Passthrough (0.0 W)";
        }
        else
        {
            PowerRateText = "Idle";
        }

        StatusDescription = (IsPluggedIn, IsCharging) switch
        {
            (true, true) => $"Charging ({BatteryPercentage}%) • AC Connected",
            (true, false) => $"Plugged in • Charge gated at {SelectedThreshold}%",
            (false, _) => $"Discharging ({BatteryPercentage}%) • Battery Power"
        };

        // Update active preset badge
        foreach (var item in ThresholdOptions)
        {
            item.IsActive = (item.Percentage == SelectedThreshold);
        }
    }

    private void OnAlertTriggered(string message)
    {
        Application.Current?.Dispatcher?.Invoke(() =>
        {
            StatusFeedback = message;
        });
    }

    partial void OnStartWithWindowsChanged(bool value)
    {
        AutoStartService.SetAutoStart(value);
    }

    [RelayCommand]
    public async Task SetThresholdAsync(int percentage)
    {
        if (_isApplying) return;
        _isApplying = true;

        int previousThreshold = SelectedThreshold;
        string previousModeTitle = ActiveModeTitle;

        // 1. Instant Optimistic UI Update (0ms latency, eliminates UI freeze)
        SelectedThreshold = percentage;
        var selectedOpt = ThresholdOptions.FirstOrDefault(t => t.Percentage == percentage);
        if (selectedOpt != null)
        {
            ActiveModeTitle = selectedOpt.Label;
        }

        foreach (var item in ThresholdOptions)
        {
            item.IsActive = (item.Percentage == percentage);
        }

        StatusDescription = (IsPluggedIn, IsCharging) switch
        {
            (true, true) => $"Charging ({BatteryPercentage}%) • AC Connected",
            (true, false) => $"Plugged in • Charge gated at {percentage}%",
            (false, _) => $"Discharging ({BatteryPercentage}%) • Battery Power"
        };

        StatusFeedback = $"Applying {percentage}% limit in BIOS...";

        // 2. Execute WMI in background thread without stalling the UI message pump
        bool success = false;
        try
        {
            success = await Task.Run(() => _batteryManager.SetThresholdAsync(percentage));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MainViewModel] SetThreshold error: {ex.Message}");
            success = false;
        }
        finally
        {
            _isApplying = false;
        }

        if (success)
        {
            StatusFeedback = $"Active: {percentage}% limit successfully saved to BIOS!";

            var window = Application.Current?.MainWindow;
            if (window != null)
            {
                MessageBox.Show(
                    window,
                    $"Battery charge threshold set to {percentage}% ({ActiveModeTitle})!\n\nYour laptop BIOS will now cap charging at this level.",
                    "ChargeGate",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show(
                    $"Battery charge threshold set to {percentage}% ({ActiveModeTitle})!\n\nYour laptop BIOS will now cap charging at this level.",
                    "ChargeGate",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
        else
        {
            // Revert optimistic UI on failure
            SelectedThreshold = previousThreshold;
            ActiveModeTitle = previousModeTitle;
            foreach (var item in ThresholdOptions)
            {
                item.IsActive = (item.Percentage == previousThreshold);
            }

            StatusFeedback = "Failed to apply threshold. Administrator privileges may be required.";

            var window = Application.Current?.MainWindow;
            if (window != null)
            {
                MessageBox.Show(
                    window,
                    "Unable to apply the charge threshold.\n\nPlease ensure ChargeGate is run as Administrator, or that your BIOS allows WMI management.",
                    "ChargeGate Warning",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            else
            {
                MessageBox.Show(
                    "Unable to apply the charge threshold.\n\nPlease ensure ChargeGate is run as Administrator, or that your BIOS allows WMI management.",
                    "ChargeGate Warning",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
    }

    [RelayCommand]
    public async Task ToggleTravelModeAsync()
    {
        if (IsTravelMode)
        {
            StatusFeedback = "Canceling Travel Mode...";
            bool success = await Task.Run(() => _batteryManager.CancelTravelModeAsync());
            if (success)
            {
                IsTravelMode = false;
                StatusFeedback = $"Travel Mode canceled. Charge limit restored to {_batteryManager.TravelRestoreThreshold}%.";
            }
        }
        else
        {
            StatusFeedback = "Activating Travel Mode (100% Top-Up)...";
            bool success = await Task.Run(() => _batteryManager.ActivateTravelModeAsync());
            if (success)
            {
                IsTravelMode = true;
                StatusFeedback = "Travel Mode active: Charging to 100%. Will automatically revert to 80% on next unplug!";
            }
        }
    }

    [RelayCommand]
    public async Task GenerateBatteryReportAsync()
    {
        StatusFeedback = "Generating detailed battery diagnostic report...";
        string? reportPath = await BatteryReportService.GenerateReportAsync();
        if (!string.IsNullOrEmpty(reportPath))
        {
            StatusFeedback = "Battery diagnostic report opened in browser.";
            BatteryReportService.OpenReport(reportPath);
        }
        else
        {
            StatusFeedback = "Failed to generate report. powercfg may require elevated permissions.";
        }
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        StatusFeedback = "Refreshing battery state...";
        var snap = await Task.Run(() => _batteryManager.RefreshSnapshotAsync());
        UpdateFromSnapshot(snap);
        StatusFeedback = "Battery state updated.";
    }

    [RelayCommand]
    public void ToggleTheme()
    {
        CurrentTheme = CurrentTheme switch
        {
            "System" => "Dark",
            "Dark" => "Light",
            "Light" => "System",
            _ => "System"
        };

        if (Application.Current?.MainWindow is MainWindow mw)
        {
            mw.ApplyTheme(CurrentTheme);
        }

        StatusFeedback = $"Theme switched to: {CurrentTheme}";
    }

    [RelayCommand]
    public void SwitchGraphMode(string mode)
    {
        GraphMode = mode;
        GraphModeLabel = mode == "Watts" ? "Power Flow (Watts)" : "Battery Level (%)";
    }

    private void OnHistoryUpdated()
    {
        Application.Current?.Dispatcher?.Invoke(() =>
        {
            var history = _batteryManager.TelemetryService.GetHistory();
            TelemetryHistory = history.ToList();

            var svc = _batteryManager.TelemetryService;
            CurrentPowerWatts = svc.CurrentWatts switch
            {
                > 0 => $"+{svc.CurrentWatts:F1} W",
                < 0 => $"{svc.CurrentWatts:F1} W",
                _ => "0.0 W"
            };

            AverageDrainText = svc.AverageDrainWatts > 0 ? $"{svc.AverageDrainWatts:F1} W" : "--";
            PeakChargeText = svc.PeakChargingWatts > 0 ? $"+{svc.PeakChargingWatts:F1} W" : "--";
        });
    }

    [RelayCommand]
    public void OpenGitHub()
    {
        OpenUrl("https://github.com/softwarebrotherhood/ChargeGate");
    }

    [RelayCommand]
    public void OpenSponsor()
    {
        OpenUrl("https://ko-fi.com/softwarebrotherhood");
    }

    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch { }
    }
}
