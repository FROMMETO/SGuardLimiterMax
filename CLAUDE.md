# CLAUDE.md

## Project Overview

SGuardLimiterMax is a lightweight Windows desktop utility built with **WPF on .NET 8.0-windows**. When a monitored game process starts, it automatically applies system-level optimizations:

- Lower SGuard anti-cheat process priority and pin it to the last CPU core
- Raise game process priority
- Optionally remove CPU 0 from the game affinity mask
- Switch Windows power plans
- Flush DNS cache
- Adjust system timer resolution
- Provide custom game process monitoring
- Support dark/light themes, tray residence, startup, and global hotkeys

## Build Commands

```powershell
# Debug / development build
dotnet build

# Run unit tests
dotnet test

# Local release build (both standalone and framework-dependent)
.\build.ps1

# Publish a single profile
dotnet publish SGuardLimiterMax.csproj -c Release -p:PublishProfile=Standalone --nologo
```

## Architecture

```
SGuardLimiterMax/
├── Views/              WPF XAML and code-behind
├── ViewModels/         INotifyPropertyChanged implementations bridging UI and Services
├── Models/             Serializable configuration models
├── Services/           Static classes wrapping Windows APIs and system operations
├── App.xaml/.cs        Application entry, single-instance mutex, theme init
└── MainWindow.xaml/.cs Custom chrome window, tray icon, event handlers
```

All services in `Services/` are currently `static class`. Do not introduce dependency injection or instance services without an explicit plan, because `MainViewModel` and `MainWindow` call them directly.

## UI / Code-Behind Contracts (Do Not Rename)

`MainWindow.xaml` and `MainWindow.xaml.cs` are coupled by name. Renaming any of these will break the build or runtime bindings.

### Event Handlers in MainWindow.xaml.cs

- `Window_MouseLeftButtonDown`
- `BtnEnterMonitor_Click`
- `BtnMinimize_Click`
- `BtnClose_Click`
- `BtnSaveConfig_Click`
- `BtnApplyNow_Click`
- `BtnApplyPlan_Click`
- `BtnRefreshPlans_Click`
- `BtnApplyTimer_Click`
- `BtnAddGame_Click`
- `BtnRemoveGame_Click`

### Named Elements in MainWindow.xaml

- `TxtProcessName`
- `TxtDisplayName`
- `ChkBoostPriority`
- `ChkUnbindCpu0`

### ViewModel Binding Paths

The following properties on `MainViewModel` are bound in XAML:

- `ThrottleSGuard`
- `BoostGamePriority`
- `UnbindCPU`
- `OptimizePower`
- `FlushDNS`
- `TimerResolution`
- `AutoMinimizeOnGame`
- `ExitWithGame`
- `AutoStart`
- `ShowNotifications`
- `RestorePowerOnExit`
- `RestoreTimerOnExit`
- `AvailablePowerPlans`
- `SelectedTargetPlan`
- `TimerResolutionOptions`
- `SelectedTimerResolutionOption`
- `CustomGames`
- `IsGameRunning`
- `StatusText`

## Important Behavioral Notes

- The app ships with a `requireAdministrator` application manifest. Local debugging must be run as administrator, or process-priority / power-plan calls will silently fail.
- A named mutex in `App.xaml.cs` enforces a single instance. If the process crashes without releasing the mutex, you may need to terminate the leftover process manually.
- `Config.json` is created next to the executable at runtime. It is portable and user-specific.
- `power_diag.log` is written next to the executable when power-plan operations occur. It is size-capped to 1 MB with a single backup (`power_diag.log.1`).

## Files That Must Not Be Committed

- `Config.json` — runtime user config
- `power_diag.log`, `power_diag.log.1` — runtime diagnostics
- `bin/`, `obj/`, `publish/`
- `.vs/`, `.idea/`
- `.claude/` — local Claude Code settings

These are already covered by `.gitignore`; do not add exceptions without discussion.

## Release Process

GitHub Actions (`.github/workflows/release.yml`) builds every push / PR and creates a Release on tag pushes (`v*`):

1. `dotnet restore`
2. `dotnet build --no-restore`
3. `dotnet test --no-build`
4. `dotnet publish` for `Standalone` and `Framework` profiles
5. Rename outputs to `SGuardLimiterMax_standalone.exe` and `SGuardLimiterMax_framework.exe`
6. Generate `SHA256SUMS.txt`
7. Upload artifacts and create GitHub Release

Always tag releases with `v{Major}.{Minor}.{Patch}` matching `SGuardLimiterMax.csproj` `<Version>`.

## Testing

Tests live in `SGuardLimiterMax.Tests/` and use **xUnit**. Prefer testing pure logic and parsing helpers over real system calls that require admin rights (e.g. `powercfg`, `NtSetTimerResolution`, process priority). If you add new services, expose parsing or decision logic as `internal static` methods so they can be unit-tested.

## Security Boundary

The tool only uses documented Windows APIs:

- `System.Diagnostics.Process`
- `powercfg.exe`
- `ipconfig /flushdns`
- `ntdll.dll` timer resolution APIs
- `schtasks.exe` for startup registration

Do **not** add code injection, memory reading, game-file modification, or driver installation features.
