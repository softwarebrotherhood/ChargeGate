using System.Collections.Concurrent;
using ChargeGate.Core.Models;

namespace ChargeGate.Core.Services;

public class BatteryTelemetryService
{
    private readonly object _lock = new();
    private readonly List<BatteryTelemetryPoint> _history = new();
    private const int MaxHistoryPoints = 180; // ~12 minutes at 4-second intervals

    public event Action? HistoryUpdated;

    public double CurrentWatts { get; private set; }
    public double PeakChargingWatts { get; private set; }
    public double PeakDrainWatts { get; private set; }
    public double AverageDrainWatts { get; private set; }

    public void RecordSnapshot(BatterySnapshot snapshot)
    {
        var point = new BatteryTelemetryPoint
        {
            Timestamp = DateTime.UtcNow,
            Percentage = snapshot.ChargePercentage,
            RateWatts = snapshot.RateWatts,
            VoltageVolts = snapshot.VoltageVolts,
            IsCharging = snapshot.IsCharging,
            IsDischarging = snapshot.IsDischarging,
            IsPluggedIn = snapshot.IsPluggedIn
        };

        lock (_lock)
        {
            // Seed a small baseline at startup if history is brand new
            if (_history.Count == 0)
            {
                for (int i = 5; i >= 1; i--)
                {
                    _history.Add(new BatteryTelemetryPoint
                    {
                        Timestamp = DateTime.UtcNow.AddSeconds(-i * 4),
                        Percentage = snapshot.ChargePercentage,
                        RateWatts = snapshot.RateWatts,
                        VoltageVolts = snapshot.VoltageVolts,
                        IsCharging = snapshot.IsCharging,
                        IsDischarging = snapshot.IsDischarging,
                        IsPluggedIn = snapshot.IsPluggedIn
                    });
                }
            }

            _history.Add(point);

            while (_history.Count > MaxHistoryPoints)
            {
                _history.RemoveAt(0);
            }

            // Recalculate stats
            CurrentWatts = point.RateWatts;

            if (point.RateWatts > PeakChargingWatts)
            {
                PeakChargingWatts = point.RateWatts;
            }

            if (point.RateWatts < 0 && Math.Abs(point.RateWatts) > PeakDrainWatts)
            {
                PeakDrainWatts = Math.Abs(point.RateWatts);
            }

            var drainPoints = _history.Where(p => p.RateWatts < 0).Select(p => Math.Abs(p.RateWatts)).ToList();
            AverageDrainWatts = drainPoints.Count > 0 ? drainPoints.Average() : 0;
        }

        HistoryUpdated?.Invoke();
    }

    public IReadOnlyList<BatteryTelemetryPoint> GetHistory()
    {
        lock (_lock)
        {
            return _history.ToList();
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            _history.Clear();
            PeakChargingWatts = 0;
            PeakDrainWatts = 0;
            AverageDrainWatts = 0;
            CurrentWatts = 0;
        }
        HistoryUpdated?.Invoke();
    }
}
