# SystemPulse

A Windows desktop app that monitors battery health and GPU temperature, detects usage drift over time, and alerts you to hardware degradation before it becomes a problem.

## Features

- Tracks battery health and GPU temperature over time
- Detects unusual usage drift and flags it
- Alerts you to signs of hardware degradation
- Runs quietly in the background with automatic startup
- Logs readings locally for historical trend tracking

## Tech Stack

- C# / WPF
- SQLite
- LibreHardwareMonitor

## Getting Started

### Requirements

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download)

### Build and run from source

```bash
git clone https://github.com/williamgoss-g1thub/SystemPulse.git
cd SystemPulse
dotnet build
dotnet run --project SystemPulse
```

### Installer

SystemPulse is also packaged as a distributable Windows installer, which sets up the app with automatic background startup — no manual configuration needed.

## License

All rights reserved.
