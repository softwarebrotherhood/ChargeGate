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

    public ObservableCollection<ThresholdOption> ThresholdOptions { get; } = new();

    public MainViewModel()
    {
        _batteryManager = new BatteryManager();
        _batteryManager.SnapshotUpdated += OnSnapshotUpdated;
        _batteryManager.AlertTriggered += OnAlertTriggered;

        HardwareInfo = $"{_batteryManager.MachineManufacturer} {_batteryManager.MachineModel} • {_batteryManager.ActiveProvider.ProviderName}";
        
        foreach (var opt in _batteryManager.ActiveProvider.SupportedThresholds)
        {
            ThresholdOptions.Add(opt);
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

        StatusDescription = (IsPluggedIn, IsCharging) switch
        {
            (true, true) => $"Charging ({BatteryPercentage}%) • Plugged into AC",
            (true, false) => $"Plugged in • Battery capped at {SelectedThreshold}%",
            (false, _) => $"Discharging ({BatteryPercentage}%) • Running on battery"
        };
    }

    private void OnAlertTriggered(string message)
    {
        Application.Current?.Dispatcher?.Invoke(() =>
        {
            StatusFeedback = message;
        });
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
        }
        else
        {
            StatusFeedback = "Failed to apply threshold. Administrator privileges may be required.";
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
        OpenUrl("https://github.com/your-username/ChargeGate");
    }

    [RelayCommand]
    public void OpenSponsor()
    {
        OpenUrl("https://github.com/sponsors/your-username");
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
