namespace ChargeGate.Core.Models;

public record ThresholdOption(
    int Percentage,
    string Label,
    string Description,
    string RawVendorValue = ""
);
