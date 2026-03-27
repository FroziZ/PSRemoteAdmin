# PSRemoteAdmin

A WPF desktop application for Windows that lets an administrator browse Active Directory, select machines at the OU or individual level, and execute PowerShell commands or scripts against them via WinRM — with streaming per-machine results.

## Features

- **Active Directory tree** — browse OUs with lazy-loading and tri-state checkbox selection (check an OU to select all computers beneath it)
- **Target machine list** — async online/offline status check per machine via TCP probe on the WinRM port
- **Command input** — Manual mode (editable textbox) or File mode (browse and load a `.ps1` script)
- **Parallel execution** — runs against all selected machines concurrently with configurable throttle; results stream back as each machine completes
- **Results panel** — per-machine stdout, stderr, exit code, duration, and timestamp; collapsible rows
- **Settings** — LDAP connection string, domain, WinRM port, max concurrency, optional RunAs credentials (stored in the Windows Credential Locker)
- **Structured logging** — Serilog rolling file log at `%LOCALAPPDATA%\PSRemoteAdmin\logs\`
- **Dark theme** — modern dark UI built entirely with WPF ResourceDictionary styles

## Requirements

- Windows 10 1903 or later (Windows 11 recommended)
- .NET 10 runtime (or use the self-contained publish)
- WinRM enabled on target machines
- Network access to your Active Directory domain controller

## Getting Started

### Build from source

```bash
git clone https://github.com/FroziZ/PSRemoteAdmin.git
cd PSRemoteAdmin
dotnet build
dotnet run --project src/PSRemoteAdmin
```

### Publish as a single executable

```bash
dotnet publish src/PSRemoteAdmin -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish/
```

Then run `publish\PSRemoteAdmin.exe` — no .NET installation required on the target machine.

## First Run

1. Open **Settings** (toolbar, top right)
2. Enter your **LDAP Connection String** — e.g. `LDAP://DC=corp,DC=local`
3. Enter your **Domain** (NetBIOS name or FQDN)
4. Optionally set a **Run As** username and password (stored securely in the Windows Credential Locker)
5. Click **Test Connection** to verify, then **Save**
6. The AD tree loads automatically

## Usage

1. Expand OUs in the left panel — children load on first expand
2. Check individual machines or entire OUs (tri-state: checked / unchecked / indeterminate)
3. Selected machines appear in the center panel with live online/offline status
4. Type a PowerShell command in the bottom editor, or switch to **File** mode and browse for a `.ps1` script
5. Click **Execute** — results stream into the right panel as each machine completes
6. Expand a result row to see full stdout and stderr output

## Project Structure

```
PSRemoteAdmin.sln
└── src/
    ├── PSRemoteAdmin.Core/       # .NET 10 class library — no UI dependencies
    │   ├── Models/               # AdNode, MachineTarget, ExecutionResult, AppSettings
    │   ├── Services/             # AD, execution, and status service interfaces + implementations
    │   └── Configuration/        # AppSettingsProvider (settings.json)
    └── PSRemoteAdmin/            # WPF app (net10.0-windows)
        ├── ViewModels/           # MVVM ViewModels (CommunityToolkit.Mvvm)
        ├── Views/                # MainWindow, SettingsWindow
        ├── Converters/           # IValueConverter implementations
        ├── Services/             # CredentialService (Windows Credential Locker)
        └── Themes/               # Dark.xaml ResourceDictionary
```

## Settings Storage

| Data | Location |
|------|----------|
| Settings file | `%APPDATA%\PSRemoteAdmin\settings.json` |
| RunAs password | Windows Credential Locker (`PSRemoteAdmin` resource) |
| Log files | `%LOCALAPPDATA%\PSRemoteAdmin\logs\app-YYYYMMDD.log` |

## Tech Stack

- **.NET 10 / C#** — WPF (`net10.0-windows10.0.19041.0`)
- **CommunityToolkit.Mvvm 8.x** — `[ObservableProperty]`, `[RelayCommand]`, source generators
- **Microsoft.PowerShell.SDK 7.4.x** — in-process PowerShell runspaces, `WSManConnectionInfo`
- **System.DirectoryServices 9.x** — LDAP / Active Directory queries
- **Microsoft.Extensions.Hosting 9.x** — dependency injection container
- **Serilog 4.x** — structured logging with rolling file sink
- **Windows.Security.Credentials** — Windows Credential Locker for RunAs passwords

## License

MIT
