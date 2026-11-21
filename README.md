# ChargeGate

[![Ko-fi](https://img.shields.io/badge/Ko--fi-Support%20SoftwareBrotherhood-F16061?style=for-the-badge&logo=ko-fi&logoColor=white)](https://ko-fi.com/softwarebrotherhood)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg?style=for-the-badge)](LICENSE)
[![.NET 9](https://img.shields.io/badge/.NET-9.0-purple.svg?style=for-the-badge)](https://dotnet.microsoft.com/)

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
- **System Tray Integration**: Minimizes quietly to the system tray with a double-click restore and right-click quick toggles (80% / 100%).
- **Start with Windows**: Optional toggle to launch automatically on login.
- **Battery Health Analytics**: Displays real-time wear percentage, design capacity vs full charge capacity in Watt-hours (Wh).
- **Fluent Windows 11 UI**: Built with `WPF-UI` with graceful fallback on Windows 10.
- **Zero Background Overhead**: Lightweight native .NET 9 footprint (~60 MB RAM).

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

## Support & Donations

If you find ChargeGate useful, please consider supporting independent open-source development:

[![Support on Ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/softwarebrotherhood)

- **Donate via Ko-fi**: [https://ko-fi.com/softwarebrotherhood](https://ko-fi.com/softwarebrotherhood)
- Star the repository to help other laptop users discover the project!
