# RC Drag Manager

A Windows desktop app that lets a Race Director run NHRA-style RC drag racing
tournaments — one operator, one machine, no network required, and no
auto-advancement: every step is a deliberate click.

## Features

- **Race modes**: NHRA Pro Ladder (seeded, 3–24 drivers), Random draw brackets,
  and QMDRA-style Round Robin with standings, buybacks, Losers Bracket and Finals.
- **Multi-class events**: run several classes side by side in one tabbed console,
  with a shared round-robin gate and a combined event summary.
- **Driver registry**: lifetime win/loss and events-won stats, cars with
  per-class dial-ins, qualifying times.
- **Save / resume**: events persist to a local SQLite database and resume
  mid-bracket.
- **Live scoreboard (optional)**: pushes bracket state to a companion web
  scoreboard ([RCDragLiveServer](https://github.com/stewmac570)) so spectators
  can follow along; supports remote dial-in submission.
- **Dark / light theme**: flame-orange design system, switchable in Settings.

The current UI is **WPF** (`RCDragManagerProd.WPF`, shipped since v2.0.0). The
original WinForms UI remains in the solution as legacy.

## Download

Grab the latest installer (`RC-Drag-Manager-Setup-*.exe`) from
[Releases](https://github.com/stewmac570/RC-Drag-Manager/releases). Data lives
in `%APPDATA%\RC_Drag_Manager` (SQLite DB, settings, log).

## Building from source

- **Requirements**: Visual Studio 2022 (or Build Tools) with .NET Framework 4.8
  targeting pack. This is .NET Framework — `dotnet build` will not work; use
  MSBuild or Visual Studio.
- Open the repo-root `RCDragManagerProd.sln`, restore NuGet packages, set
  `RCDragManagerProd.WPF` as the startup project, F5.
- Command line:
  `MSBuild RCDragManagerProd.sln /t:Build /p:Configuration=Debug`
- Tests: `dotnet test src/RCDragManagerProd.Tests/RCDragManagerProd.Tests.csproj`
  (MSTest, in-memory/temp SQLite — no setup needed).
- Installer: `Installer/build-installer.ps1` (Inno Setup 6).

## Documentation

Architecture, domain model, data layer and race-flow docs live in
[`Docs/claude-context/`](Docs/claude-context/) — start with
`PROJECT-OVERVIEW.md` and `ARCHITECTURE.md`. `CLAUDE.md` at the repo root
defines the layering rules (Form → Service → Controller → Engine) that all
changes must follow.

## License

All rights reserved. Personal project of Stew Mac (stewmac570); no license is
currently granted for reuse or redistribution.
