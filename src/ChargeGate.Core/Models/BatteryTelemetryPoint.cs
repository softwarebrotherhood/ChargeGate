namespace ChargeGate.Core.Models;

public record BatteryTelemetryPoint
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public int Percentage { get; init; }
    public double RateWatts { get; init; }
    public double VoltageVolts { get; init; }
    public bool IsCharging { get; init; }
    public bool IsDischarging { get; init; }
    public bool IsPluggedIn { get; init; }
}
