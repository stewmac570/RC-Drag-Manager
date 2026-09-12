# RC Drag Manager — Project Overview

## What the App Does

RC Drag Manager is a Windows desktop application for managing **NHRA-style RC (radio-controlled) drag racing events**. It allows a Race Director to:

- Maintain a persistent registry of drivers and their cars
- Set up a race event (choose race type, class, and driver roster)
- Run a full bracket tournament from first round through to a champion
- Save and reload sessions mid-event
- Track cumulative driver stats across events
- Run multi-class events: one event owns one or more classes, each class is one bracket and one console tab (`MultiClassEvent` / `MultiClassEventRepository`)

The app is used **trackside at live RC drag racing events**. The Race Director sits at a laptop and manually drives every step — bracket generation, winner entry, round advancement. Nothing auto-advances; NHRA compliance requires the human to be in control.

## Who Uses It

A single **Race Director** (event organiser) running Windows. There is no multi-user, no cloud: the event runs on one machine, offline. One optional network feature exists: a live-scoreboard push to the server, off by default (`LiveApiClient` posts to stewmacrc.com).

## How It's Used at a Race Event

1. **Before the day**: drivers and cars are added via the Driver Manager.
2. **Session setup**: the Race Director creates a new session — picks race type (Pro Ladder / Random / Round Robin / Multi-Car Round Robin), class (Heads Up, Bracket Class, Dial-In), enters any qualifying times, selects the roster, and starts.
3. **Racing**: the WPF race console (`RaceConsoleWindow`) becomes the race console. The legacy WinForms `Form1` does the same job for the WinForms app, hosted one-per-class inside `MultiClassRaceForm`. The bracket is generated, and the Director enters winners match by match. "Generate Next Round" is clicked manually only when all matches in the current round are resolved.
4. **Round Robin flow**: after RR rounds complete, a Losers Bracket is offered for drivers who didn't make top-3; the LB champion joins the top-3 in a Final-4 Pro Ladder bracket.
5. **End of event**: standings are shown, session is saved (or not — it's optional), and stats are updated to the permanent driver registry.

---

## Tech Stack

| Item | Detail |
|------|--------|
| **Language** | C# |
| **Framework** | .NET Framework 4.8 |
| **UI** | WPF (primary since v2.0.0; `RCDragManagerProd.WPF`). WinForms (`UI/Forms/*`) is legacy but still builds |
| **Database** | SQLite via `System.Data.SQLite` (NuGet) |
| **Serialization** | `System.Text.Json` (built-in, .NET 4.8 backport via NuGet) |
| **Logging** | Custom `Logger.cs` writing to `%APPDATA%\RC_Drag_Manager\app.log` |
| **Tests** | MSTest 4.0.1 (the `MSTest` meta-package) targeting `net48` |
| **Installer** | Inno Setup (per-user, no UAC required) |
| **Platform** | Windows only |

### Key NuGet Packages (main project)

- `System.Data.SQLite` — SQLite driver
- `System.Text.Json` — JSON serialization
- `System.ValueTuple`, `System.Numerics.Vectors`, `System.Buffers` — .NET 4.8 backports

### Key NuGet Packages (test project)

- `MSTest` 4.0.1 (the meta-package) — test runner, targeting `net48`

---

## Solution Structure

```
RC-Drag-Manager/
├── RCDragManagerProd.sln           ← Root solution: all three projects, incl. Tests
├── src/
│   ├── RCDragManagerProd/          ← WinForms app, legacy UI, still builds
│   │   └── RCDragManagerProd.sln   ← Older solution: the two app projects only, no Tests
│   ├── RCDragManagerProd.WPF/      ← Current UI (v2.0.0+): 9 top-level windows + themed dialogs
│   ├── RCDragManagerProd.Tests/    ← Unit/integration test project
│   └── RCDragManager.CodeStats/    ← Standalone code analysis tool (not part of main app)
├── Docs/                           ← All project documentation
│   ├── claude-context/             ← This folder
│   └── DevLog/                     ← Detailed development history
└── Installer/                      ← Inno Setup scripts
```

---

## How to Build and Run

### Prerequisites

- Visual Studio 2022 (or later) with .NET desktop workload
- .NET Framework 4.8 SDK installed
- NuGet packages restore automatically on first build

### Build Steps

1. Open `RCDragManagerProd.sln` (repo root, includes all three projects and Tests) in Visual Studio.
2. Build → Rebuild Solution (NuGet packages restore automatically).
3. Output is under each project's `bin/Debug/` or `bin/Release/`: e.g. `src/RCDragManagerProd/bin/Debug/` (WinForms) and `src/RCDragManagerProd.WPF/bin/Debug/net48/` (WPF).

### Running

- F5 from Visual Studio (Debug mode).
- Or run `RCDragManagerProd.WPF.exe` directly from the WPF project's bin folder
  (the current UI; the legacy WinForms app is `RCDragManagerProd.exe`).
- On first run the app creates `%APPDATA%\RC_Drag_Manager\` and the SQLite database `race_data.db`.
- Logs go to `%APPDATA%\RC_Drag_Manager\app.log`. Logging is **ON in Debug, OFF in Release** (controlled by `appsettings.json` in the same folder).

### Running Tests

- Open `RCDragManagerProd.Tests` project in Test Explorer.
- Run All. Tests use throwaway temp-file SQLite databases in the temp folder (deleted on dispose), never `:memory:`; no external setup required.

---

## Runtime Data Locations

| Item | Path |
|------|------|
| Database | `%APPDATA%\RC_Drag_Manager\race_data.db` |
| Log file | `%APPDATA%\RC_Drag_Manager\app.log` |
| Settings | `%APPDATA%\RC_Drag_Manager\appsettings.json` |
