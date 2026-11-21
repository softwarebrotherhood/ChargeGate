# ChargeGate — "AlDente for Windows"

[![Website](https://img.shields.io/badge/Website-GitHub%20Pages-10b981?style=for-the-badge&logo=googlechrome&logoColor=white)](https://softwarebrotherhood.github.io/ChargeGate/)
[![Ko-fi](https://img.shields.io/badge/Ko--fi-Support%20SoftwareBrotherhood-F16061?style=for-the-badge&logo=ko-fi&logoColor=white)](https://ko-fi.com/softwarebrotherhood)
[![Release](https://img.shields.io/badge/Version-v1.1.0-blue.svg?style=for-the-badge)](CHANGELOG.md)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg?style=for-the-badge)](LICENSE)
[![.NET 9](https://img.shields.io/badge/.NET-9.0-purple.svg?style=for-the-badge)](https://dotnet.microsoft.com/)

**ChargeGate** is a lightweight, open-source battery charge threshold manager for Windows laptops, built with **.NET 9** and Windows 11 Fluent UI.

Prolonged battery exposure to 100% state-of-charge while plugged into wall power accelerates lithium battery degradation. ChargeGate interfaces directly with hardware ACPI and BIOS management controllers to stop charging at healthy thresholds (such as 80%), replacing heavy manufacturer bloatware.

👉 **Official Showcase Website**: [https://softwarebrotherhood.github.io/ChargeGate/](https://softwarebrotherhood.github.io/ChargeGate/)

---

## Key Features

- **Direct Hardware Control**: Controls battery charge thresholds directly via vendor ACPI/WMI without background bloatware.
- **Multi-Vendor Architecture**:
  - **HP**: Native BIOS setting interface via `root\HP\InstrumentedBIOS` (*Battery Health Manager*).
  - **ASUS**: Direct WMNB `0x00120057` method invocation (*ASUS Battery Health Charging*).
  - **Lenovo**: Supports IdeaPad, Legion (*Conservation Mode*), and ThinkPad threshold controls.
  - **Dell**: Supports DCIM / Command battery charging profiles (`root\dcim\sysman`).
  - **Acer**: Supports Acer Care Center WMI threshold registers.
  - **Universal Fallback**: Active notification and audible alert monitor for laptops without BIOS-level gating.
- **Smart Travel Mode (100% Top-Up)**:
  - Temporarily charges to 100% for travel with one click.
  - Automatically detects when you unplug your charger and seamlessly restores your 80% battery health protection limit in the BIOS.
- **Hardware Telemetry Strip**:
  - Real-time power rate in Watts ($W$): monitors charge speed, discharge rate, and AC passthrough ($0.0\text{ W}$).
  - Live cell pack voltage ($V$) from ACPI smart battery registers.
  - Hardware **Cycle Count** read directly from the battery gas gauge.
- **Dynamic Taskbar System Tray Icon**:
  - Renders the live battery percentage badge right next to the Windows clock with color-coded status rings.
- **Zero-Latency Optimistic UI**:
  - All hardware WMI operations execute asynchronously on background threads; presets switch instantaneously with zero interface lag.
- **System Tray Integration**:
  - Minimizes quietly to the tray on close, with double-click restore and quick preset toggles (80%, 100%, Travel Mode).
- **Start with Windows**:
  - Automatically launches in the background upon Windows login.

---

## Getting Started

### Option 1: Standalone Portable Executable (No Install Required)
Download the latest `ChargeGate-win-x64.zip` from [Releases](https://github.com/softwarebrotherhood/ChargeGate/releases), extract it, and run `ChargeGate.UI.exe`.

### Option 2: Build From Source
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

## Changelog
See [CHANGELOG.md](CHANGELOG.md) for full release notes and version history.

---

## Support & Donations

ChargeGate is 100% free, ad-free, and independently maintained by **softwarebrotherhood**. If ChargeGate has helped preserve your laptop's battery lifespan, please consider supporting development on Ko-fi:

[![Support on Ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/softwarebrotherhood)

- **Donate via Ko-fi**: [https://ko-fi.com/softwarebrotherhood](https://ko-fi.com/softwarebrotherhood)
- ⭐ Star the repository to help other laptop users discover the project!
