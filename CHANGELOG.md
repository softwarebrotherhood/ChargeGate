# Changelog

All notable changes to **ChargeGate** are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.4.0] - 2026-11-22

### Added
- **Real-Time Battery Usage, Drain & Charger Telemetry Graph:**
  - Added native vector graph (`BatteryGraphControl`) rendering real-time battery telemetry with zero external dependencies and smooth 60fps DirectX hardware acceleration.
  - **Power Flow (Watts):** Dual-zone visualization displaying real-time AC charger wattage ($+W$) in emerald green and battery discharge drain ($-W$) in amber, with a distinct $0.0\text{ W}$ passthrough baseline when gated at the threshold.
  - **Battery Level (%):** Visual charge curve showing battery capacity over time with gradient fill and dashed active gate limit indicator (e.g. 80% Gate).
  - **Live Metric Summary Chips:** Displays real-time live power flow ($W$), average discharge drain rate, and peak charging input.
  - **Interactive Hover Crosshair & Tooltips:** Inspect exact historical timestamps, percentages, and wattage draw by moving the cursor across the graph.
  - **Interactive Showcase Mockup:** Added live animated canvas telemetry graph simulation and dual-mode toggling to the documentation website (`docs/`).

## [1.3.0] - 2026-11-21

### Added
- **Dark and Light Appearance Theme Engine:**
  - Added real-time dynamic switching between **System**, **Dark**, and **Light** themes.
  - Automatically adapts in real-time to Windows OS theme modifications via `SystemEvents.UserPreferenceChanged`.
  - Added matching glassmorphic theme toggle on the showcase website (`docs/`) with system dark-mode preference detection.
- **Brand Identity & High-Resolution Iconography:**
  - Designed official vector shield logo (`docs/assets/logo.svg`) and multi-resolution Windows icon (`app.ico` supporting 16x16, 32x32, 48x48, 64x64, 128x128, and 256x256).
  - Integrated into Windows window chrome, dynamic system tray badge, installer setup wizard, and desktop/Start shortcuts.
- **Google Search Console Verification:**
  - Integrated site ownership verification meta tag into `docs/index.html` for Google search indexing.

### Fixed
- **Startup XAML BAML Initialization:**
  - Resolved `TypeConverterMarkupExtension` startup crash by removing raw XAML icon conversion and loading multi-resolution `app.ico` safely from WPF Pack URI resources in code.

## [1.2.0] - 2026-11-21

### Added
- **Local Windows Setup Installer Builder:**
  - Added `build-installer.ps1` and `installer/ChargeGate.iss` powered by Inno Setup.
  - Automatically compiles and outputs both `ChargeGate-Setup-v1.2.0.exe` (Windows Installer with Start Menu / Desktop shortcuts) and standalone portable `ChargeGate.exe`.
- **Battery Health Diagnostics Exporter:**
  - Added `BatteryReportService.cs` with an interactive "Report" button in the UI.
  - Generates comprehensive Windows battery degradation history and discharge statistics (`powercfg /batteryreport`) and opens directly in your browser.
- **Microsoft Surface Provider:**
  - Added `SurfaceBatteryProvider.cs` supporting Surface UEFI Battery Limit (50% Kiosk/Dock mode and 80% Smart Charging).
- **LG Gram Provider:**
  - Added `LgBatteryProvider.cs` supporting LG Control Center Battery Care 80% limit.

## [1.1.0] - 2026-11-21

### Added
- **Travel Mode (100% One-Time Top-Up):**
  - Added a dedicated quick-action card and tray command to top up battery to 100% for trips.
  - Automatically detects when AC power is unplugged and seamlessly re-engages your 80% battery health protection limit in the BIOS.
- **Hardware Telemetry Strip:**
  - Displays real-time power rate in Watts ($W$): detects charging rate, discharging draw, and AC passthrough ($0.0\text{ W}$).
  - Displays cell pack voltage ($V$) queried directly from ACPI smart battery registers.
  - Displays hardware **Cycle Count** read from `root\wmi:BatteryCycleCount`.
- **Dynamic Taskbar System Tray Icon:**
  - In-memory rendering of the live battery percentage badge right next to the Windows clock.
  - Color-coded borders: Emerald green for active charge gating, Blue for charging, Amber for discharging, and Red for low battery.
  - Interactive tooltip with live battery percentage and active gate threshold.
- **Acer Laptop Provider:**
  - Added `AcerBatteryProvider` supporting Acer Care Center WMI threshold control (80% and 100%).
- **Interactive Documentation & Showcase Webpage:**
  - Added a responsive, modern glassmorphism showcase website under `/docs` ready for immediate deployment on GitHub Pages.
- **GitHub Actions Automated Releases:**
  - Added `.github/workflows/build-and-release.yml` for automated compilation and publishing of self-contained, standalone single-file `.exe` packages.

### Changed
- **Zero-Latency Optimistic UI Engine:**
  - Applying presets now updates UI badges, status text, and cards instantaneously (0 ms perceptible latency) without waiting for the BIOS NVRAM write cycle.
- **Asynchronous Non-Blocking WMI Pipeline:**
  - Offloaded all ACPI/WMI hardware calls (`HpBatteryProvider`, `AsusBatteryProvider`, `LenovoBatteryProvider`, `DellBatteryProvider`, `AcerBatteryProvider`) to background worker threads via `Task.Run()`, keeping the WPF UI Dispatcher completely smooth and responsive.
- **WMI Query Optimization:**
  - Cached static hardware values (such as `DesignCapacity` and `CycleCount`) to cut periodic background polling overhead by over 50%.
- **Re-entrancy Guard:**
  - Added an atomic execution guard to prevent accidental duplicate BIOS writes during rapid clicks.

### Fixed
- Fixed the 3-second UI freeze and busy cursor when clicking preset Apply buttons.
- Fixed HP WMI instance binding: queries live `HP_BIOSSettingInterface` instance rather than static class methods.
- Fixed Windows 10 22H2 compatibility crash caused by backdrop mismatch on `FluentWindow`.
- Fixed symbol resource resolution error on initial launch.

---

## [1.0.0] - 2026-11-19

### Added
- Initial open-source release of **ChargeGate**.
- Native vendor ACPI/WMI integration for:
  - **HP:** *HP Battery Health Manager* (`Maximize my battery health` 80%, `Let HP manage` 85%, `Maximize duration` 100%).
  - **ASUS:** *ASUS Battery Health Charging* ATK/WMI (`0x00120057` DEVS/DSTS 60%, 80%, 100%).
  - **Lenovo:** *Conservation Mode* (IdeaPad/Legion 60%) and *ThinkPad Charge Thresholds*.
  - **Dell:** *Command | Monitor DCIM WMI* (Primarily AC Use 80%, Adaptive 85%, Standard 100%).
  - **Universal Fallback Provider:** Audio-visual smart alert notification when target charge level is reached on unsupported hardware.
- Modern Windows 11 Fluent UI dark theme built with `WPF-UI`.
- System tray integration with minimize-to-tray and background monitoring.
- Windows autostart configuration via registry.
- Integrated Ko-fi sponsorship link to support open-source development.
