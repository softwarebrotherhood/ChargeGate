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

    public ObservableCollection<ThresholdItemViewModel> ThresholdOptions { get; } = new();

    public MainViewModel()
    {
        _batteryManager = new BatteryManager();
        _batteryManager.SnapshotUpdated += OnSnapshotUpdated;
        _batteryManager.AlertTriggered += OnAlertTriggered;

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

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        _batteryManager.StartMonitoring();
        var snap = await _batteryManager.RefreshSnapshotAsync();
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
        IsBusy = true;
        StatusFeedback = $"Applying {percentage}% charge limit...";

        bool success = await _batteryManager.SetThresholdAsync(percentage);
        IsBusy = false;

        if (success)
        {
            SelectedThreshold = percentage;
            StatusFeedback = $"Successfully set battery threshold to {percentage}%!";

            var snap = await _batteryManager.RefreshSnapshotAsync();
            UpdateFromSnapshot(snap);

            MessageBox.Show(
                $"Battery charge threshold set to {percentage}% ({snap.ActiveModeLabel})!\n\nYour laptop BIOS will now cap charging at this level.",
                "ChargeGate",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        else
        {
            StatusFeedback = "Failed to apply threshold. Administrator privileges may be required.";
            MessageBox.Show(
                "Unable to apply the charge threshold.\n\nPlease ensure ChargeGate is run as Administrator, or that your BIOS allows WMI management.",
                "ChargeGate Warning",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        IsBusy = true;
        var snap = await _batteryManager.RefreshSnapshotAsync();
        UpdateFromSnapshot(snap);
        IsBusy = false;
        StatusFeedback = "Battery state updated.";
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
