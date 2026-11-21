using CommunityToolkit.Mvvm.ComponentModel;

namespace ChargeGate.UI.ViewModels;

public partial class ThresholdItemViewModel : ObservableObject
{
    public int Percentage { get; }
    public string Label { get; }
    public string Description { get; }
    public string RawVendorValue { get; }

    [ObservableProperty]
    private bool _isActive;

    public ThresholdItemViewModel(int percentage, string label, string description, string rawVendorValue = "", bool isActive = false)
    {
        Percentage = percentage;
        Label = label;
        Description = description;
        RawVendorValue = rawVendorValue;
        IsActive = isActive;
    }
}
