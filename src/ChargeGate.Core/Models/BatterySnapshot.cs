namespace ChargeGate.Core.Models;

public record BatterySnapshot
{
    public int ChargePercentage { get; init; }
    public bool IsPluggedIn { get; init; }
    public bool IsCharging { get; init; }
    public bool IsDischarging { get; init; }
    public int EstimatedRemainingMinutes { get; init; }
    public int DesignCapacityMWh { get; init; }
    public int FullChargeCapacityMWh { get; init; }
    public int HealthPercentage => DesignCapacityMWh > 0 && FullChargeCapacityMWh > 0
        ? Math.Min(100, (int)Math.Round((double)FullChargeCapacityMWh / DesignCapacityMWh * 100))
        : 100;
    public int CycleCount { get; init; } = -1;
    public double VoltageVolts { get; init; } = 0;
    public double RateWatts { get; init; } = 0;
    public int CurrentThresholdPercentage { get; init; }
    public string ActiveModeLabel { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
