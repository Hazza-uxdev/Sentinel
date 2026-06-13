# Sentinel

Sentinel is a modern, local-first Windows security triage tool built with WPF (.NET).
It helps users inspect suspicious downloads, review static file indicators, and spot common LOLBin command-line abuse.

Sentinel is not an antivirus.

It never executes suspicious files, never deletes files, never modifies system files, and never terminates processes automatically.

Your findings stay on your device.

---

## Features

- Modern Windows desktop UI inspired by Bastion
- Dashboard with security status and local triage statistics
- Download Inspector for recently downloaded files
- Background Downloads monitoring with tray support
- Windows toast-style tray alerts for suspicious downloads
- Local risk scoring with plain-English explanations
- Safe overrides for trusted downloads, analyzed files, and activity alerts
- File Analyzer for hashes, file type risk, and static metadata
- Activity Monitor for suspicious LOLBin command lines
- Encoded PowerShell command extraction and copy support
- Themed right-click menus for app actions
- Local YARA rules folder for adding detection files
- JSON and HTML report export
- Local JSON-backed settings and history storage

---

## System Requirements

- Windows 10 or Windows 11
- .NET 8.0 SDK or newer
- Visual Studio 2022 or the .NET CLI

---

## Installation

### Option 1: Run from Source

1. Clone the repository

   ```powershell
   git clone https://github.com/your-username/sentinel.git
   cd sentinel
   ```

2. Restore dependencies

   ```powershell
   dotnet restore Sentinel.csproj
   ```

3. Run the app

   ```powershell
   dotnet run --project Sentinel.csproj
   ```

---

### Option 2: Build a Standalone Executable

1. Open the project in Visual Studio 2022

2. Set build configuration

   ```text
   Release | x64
   ```

3. Build the project

   ```powershell
   dotnet publish Sentinel.csproj -c Release -r win-x64 --self-contained false
   ```

4. Locate the output

   ```text
   bin/Release/net8.0-windows/win-x64/publish/
   ```

5. Run

   ```text
   Sentinel.exe
   ```

---

## First Launch

- Sentinel opens to the dashboard.
- The Downloads folder path is read from your Windows profile.
- Background monitoring can be enabled from Settings.
- Closing the window hides Sentinel to the tray so monitoring can continue.
- Use Quit Sentinel to fully exit the app.

---

## Core Modules

### Download Inspector

Sentinel watches your Downloads folder and inspects new or existing files.

It checks for:

- Executable file types
- Script files
- Archives
- Double extensions
- Very long filenames
- Random-looking filenames
- Suspicious archive names

Files can be marked safe from the right-click menu. Safe files are ignored in future scoring.

### File Analyzer

Sentinel can manually inspect a selected file without running it.

It displays:

- File name and path
- File size
- MD5, SHA1, and SHA256 hashes
- Category
- Risk score
- Plain-English explanation

### Activity Monitor

Sentinel scans running processes for known LOLBins and suspicious command-line patterns.

Examples include:

- powershell.exe
- pwsh.exe
- cmd.exe
- certutil.exe
- bitsadmin.exe
- mshta.exe
- regsvr32.exe
- rundll32.exe
- wscript.exe
- cscript.exe
- wmic.exe
- msiexec.exe

Encoded PowerShell commands can be copied for investigation. Activity alerts can also be marked safe.

---

## Data Storage

Sentinel stores settings and findings locally on your PC.

Main local data folder:

```text
%LOCALAPPDATA%/Sentinel/
```

This may include:

- App settings
- Download findings
- Activity alerts
- Safe overrides
- Local report history

Reports are exported to:

```text
Reports/
```

YARA rules can be added to:

```text
YaraRules/
You can also find pre-found yara rules for a RAT from the following website: https://trojandb.org/browse
```

---

## Risk Scoring

Sentinel uses a local scoring model from 0 to 100.

```text
0-20    Safe
21-40   Low
41-60   Medium
61-80   High
81-100  Critical
```

Example score increases:

- Downloaded executable: +15
- Script file: +20
- Double extension: +30
- Random-looking filename: +10
- Suspicious archive name: +10

Scores are explanations, not verdicts. A high score means the item deserves attention, not that it is confirmed malware.

---

## Privacy

- No cloud scanning
- No telemetry
- No account required
- No uploaded files
- No remote API dependency
- Findings remain local unless you export a report yourself

---

## Security Disclaimer

Sentinel is a defensive investigation tool.

It is designed to help users understand suspicious local activity, but it does not replace antivirus software, EDR, backups, patching, or professional incident response.

Sentinel does not:

- Execute suspicious files
- Quarantine files
- Delete files
- Modify system files
- Terminate processes automatically
- Guarantee that a file is safe or malicious

---

## Tech Stack

- C# (.NET 8)
- WPF (XAML)
- System.Management for process command-line inspection
- System.Windows.Forms NotifyIcon for tray support
- Local JSON storage
- SHA256, SHA1, and MD5 hashing

---

## Project Structure

```text
Sentinel/
  Assets/
  Data/
  Models/
  Services/
  Reports/
  YaraRules/
  App.xaml
  App.xaml.cs
  GlobalUsings.cs
  MainWindow.xaml
  MainWindow.xaml.cs
  Sentinel.csproj
  README.md
```

---

## Roadmap

- Full PE metadata parser
- Digital signature verification
- Native Windows toast notifications
- More LOLBin rules loaded from JSON
- YARA rule execution against selected files
- CSV report export
- Installer packaging
- More dashboard filtering and sorting
- Light theme support

---

## License

MIT License

---

## Credits

Built as a local-first Windows security triage app with safety, clarity, and privacy in mind.
