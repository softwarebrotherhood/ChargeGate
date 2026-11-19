# ChargeGate

**ChargeGate** is a lightweight, open-source battery charge threshold manager for Windows laptops, built with **.NET 9** and Windows 11 Fluent UI.

Prolonged battery exposure to 100% state-of-charge while plugged into wall power accelerates lithium battery degradation. ChargeGate interfaces directly with hardware ACPI and BIOS management controllers to stop charging at healthy thresholds (such as 80%), replacing heavy manufacturer bloatware.

---

## Features

- **Direct Hardware Control**: Controls battery charge thresholds directly via vendor ACPI/WMI without background bloat.
- **Multi-Vendor Architecture**:
  - **HP**: Interfaces via `root\HP\InstrumentedBIOS` (Battery Health Manager).
  - **ASUS**: Supports ROG, TUF, and ZenBook laptops via ATK/WMI.
  - **Lenovo**: Supports IdeaPad, Legion (Conservation Mode), and ThinkPad threshold controls.
  - **Dell**: Supports DCIM / Command battery charging profiles.
  - **Universal Fallback**: Active notification and audible alert monitor for laptops without BIOS-level gating.
- **Fluent Windows 11 UI**: Built with `WPF-UI` and Mica backdrop support.
- **Zero Background Overhead**: Lightweight native .NET 9 footprint.

---

## Getting Started

### Prerequisites
- Windows 10 (1809+) or Windows 11
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

### Build & Run
```powershell
# Clone the repository
git clone https://github.com/softwarebrotherhood/ChargeGate.git
cd ChargeGate

# Build the solution
dotnet build

# Run the UI
dotnet run --project src/ChargeGate.UI/ChargeGate.UI.csproj
```

---

## Contributing & Support

Contributions, bug reports, and hardware profile additions are welcome! Please feel free to submit an issue or pull request.

If you find ChargeGate useful, consider starring the repo or supporting the project via donations.
