# Changelog

All notable changes to **ChargeGate** are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

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
