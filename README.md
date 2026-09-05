# WinTrim

[![Build](https://github.com/VictorAlmeidaSiLva/WinTrim/actions/workflows/build.yml/badge.svg)](https://github.com/VictorAlmeidaSiLva/WinTrim/actions/workflows/build.yml)

A small Windows tray utility that folds a live hardware overlay, startup/service management, and RAM/VRAM tools into one dependency-free app.

## Download

Grab the latest `WinTrim.exe` from the [Releases page](https://github.com/VictorAlmeidaSiLva/WinTrim/releases/latest) — no installer, no .NET runtime to install, just run it. Windows SmartScreen may warn about an unsigned executable from an unrecognized publisher on first run ("More info" → "Run anyway") since this isn't code-signed.

## Why

Doing all of this normally means running several separate tools: MSI Afterburner/RTSS for a CPU/GPU/RAM overlay, Autoruns or Task Manager for startup and service management, a memory optimizer for RAM cache purging, and Task Manager's GPU tab for per-process VRAM usage. WinTrim folds the essentials of all of that into a single lightweight tray app, with no installer, no bundled bloatware, and no telemetry — everything runs locally.

## Features

**Overlay**
- Always-on-top, draggable CPU / RAM / VRAM readout that reposition itself where you last dropped it
- Global hotkey to show/hide it, fully configurable from the app (no more hardcoded Ctrl+Shift+V)
- Adjustable opacity

**Services**
- WMI-backed list of Windows services (auto-start only, or all)
- A built-in catalog flags ~30 services that are actually critical to Windows (RPC, DNS Client, DHCP, Firewall, WMI, Task Scheduler, etc.) with a plain-language explanation of what each one does, shown before you disable or stop it — so you don't accidentally knock out networking or logon
- Search/filter, RAM usage per service

**Startup Programs**
- Covers `Run` / `Run32` registry keys and both Startup folders (per-user and machine-wide)
- A small catalog recognizes common vendor entries (OneDrive, GPU utilities, Steam, Discord, etc.) and tells you what they are

**Scheduled Tasks**
- Lists logon/boot-triggered scheduled tasks via COM (`Schedule.Service`) and lets you enable/disable them

**RAM cleanup**
- Purges the working-set, modified page list, and standby list via the native `NtSetSystemInformation` call — the same mechanism tools like RAMMap use, wired up with privilege enabling instead of a third-party binary

**VRAM**
- Top processes by dedicated GPU memory, read the same way Task Manager's GPU column does (`GPU Process Memory` performance counters)
- Per-app GPU preference (System default / Power saving / High performance), backed by the same registry key Windows Graphics Settings uses — no elevation needed, it's a per-user setting
- Hardware-accelerated GPU Scheduling toggle (needs a restart to apply)
- Close a heavy process directly from the list — with a hard block on system-critical processes (`dwm`, `csrss`, `winlogon`, etc.) whose termination could crash the whole session, not just "lose unsaved work"

**On-demand elevation**
- The app runs unprivileged by default. When an action needs admin rights (changing a service, purging RAM, etc.), it re-launches *itself* with a hidden `--action <name> ...` argument and a UAC prompt, does that one thing, and exits — it never runs elevated as a whole

**Settings**
- English by default, with a one-click Brazilian Portuguese option (flag-labeled language picker); switching restarts the app to apply cleanly
- Overlay position, opacity, hotkey, and language are saved to `%APPDATA%\WinTrim\config.json`
- The JSON reader/writer is hand-written (a few dozen lines) specifically to avoid pulling in a JSON library for a handful of flat fields

## Tech highlights

- WMI queries (`System.Management`) for service enumeration and control
- P/Invoke into `user32.dll` (global hotkeys), `ntdll.dll` (`NtSetSystemInformation`), and `advapi32.dll` (privilege enabling via `AdjustTokenPrivileges`)
- COM interop through `dynamic` and late-bound `Schedule.Service` / `WScript.Shell` — no type library reference needed
- Self-elevation pattern via a hidden CLI action mode instead of an always-elevated manifest, so the app stays a normal, non-admin process the rest of the time
- Dependency-free config persistence (no JSON library) and a hand-rolled two-language string table (no .resx/satellite assemblies)
- Per-process GPU memory via the `GPU Process Memory` performance counter category, and GPU preference via the undocumented-but-stable `UserGpuPreferences` registry key

## Requirements & build

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (only needed to build from source — the released `.exe` needs nothing installed)

```
dotnet build
dotnet run
```

The released binary is instead built with the classic `csc.exe` compiler (bundled with Windows) against the same source, producing a small executable with no .NET runtime dependency at all — see `.github/workflows/build.yml`.

## Project structure

```
WinTrim.csproj
Program.cs                    # entry point
src/
  Config/                     # AppConfig (persisted settings), hotkey modifier helpers
  Elevation/                  # UAC self-elevation + the hidden --action CLI mode
  Localization/               # Loc (two-language string table) and the Lang enum
  Catalog/                    # built-in "what does this do" descriptions for services/startup items
  Services/                   # WMI service enumeration and control
  Startup/                    # Run/Run32/Startup-folder registry handling
  Tasks/                      # Scheduled Task Scheduler COM interop
  Ram/                        # memory diagnostics + native purge
  Vram/                       # per-process GPU memory, GPU preference, HAGS toggle
  AutoStart/                  # "start WinTrim with Windows" shortcut management
  Ui/                         # tray icon, overlay window, main panel (split by section)
```

## Screenshots

<!-- add screenshots/overlay.png, screenshots/services.png here -->

## License

MIT — see [LICENSE](LICENSE).
