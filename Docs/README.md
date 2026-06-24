# RC Drag Manager Documentation Index

Last updated: 2026-06-06

Use these docs as the current source of truth:

| Doc | Purpose |
| --- | --- |
| `02_System_Overview.md` | Current desktop + live-server product architecture. |
| `03_Controller_Engine_Contracts.md` | Current `RaceController`, `IRaceEngine`, and engine contract. |
| `05_Mode_RoundRobin_Spec.md` | Current Round Robin/QMDRA behavior. |
| `06_SQLite_Schema.md` | Current SQLite tables. |
| `07_Repository_Contracts.md` | Current repository behavior. |
| `08_UI_UX_Surface_Map.md` | Current WinForms surfaces and public scoreboard UI boundary. |
| `09_Error_Handling_Logging.md` | Current logging and error handling. |
| `10_Race_Log_and_Reporting.md` | Current result storage plus reporting gaps. |
| `11_Installer_Packaging.md` | Current installer script behavior. |
| `12_Live_Server_Integration.md` | Shared contract between desktop app and `RCDragLiveServer`. |

## Historical Or Planning Docs

The repo also contains handover files, dev logs, QA plans, Claude context, and feature specs. Keep them for history, but do not treat them as current source of truth without checking the code.

Notably:

- `PROJECT_STATUS.md` is historical and contains old claims about missing Random/Round Robin/session persistence work.
- `DevLog/*` is historical narrative.
- `claude-context/*` is research/context from prior sessions.
- `PORTATREE-*` files describe setup/planned integration work, not implemented timing ingestion.
- `LiveIntegration*` docs are implementation/review notes from the live scoreboard work.

When adding a new canonical doc, add it to the table above. When writing a planning doc, label it clearly as planned or historical.
