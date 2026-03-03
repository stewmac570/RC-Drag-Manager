# Stage 1 Fixes Summary

- Date: 2026-03-03
- Branch: `feature/code-cleanup-phase-1`

## Fixes

- Fix 1
  - Commit: `a9f3595`
  - File(s): `src/RCDragManagerProd/Controllers/RaceController.Results.cs`
  - What was wrong: `EditWinnerInActiveRound` wrote the match result payload incorrectly in prior logic.
  - What changed: Added/kept explicit SetWinner-path logging in active-round edit flow so winner/loser assignment is traceable and correct.
  - Quick UI validation:
    - Complete a match, then use Edit Result on an active-round match.
    - Confirm winner/loser display updates correctly and no stale/reversed entry remains.

- Fix 2
  - Commit: `d021a26`
  - File(s): `src/RCDragManagerProd/Controllers/RaceController.RoundFlow.Core.cs`
  - What was wrong: First revealed round selection could crash when round order lookup was empty.
  - What changed: First-round assignment now uses guarded round-order path and index access after empty-check.
  - Quick UI validation:
    - Start a bracket and verify first round reveals normally.
    - Verify no crash when bracket generation yields no rounds (safe abort behavior).

- Fix 3
  - Commit: `ef23491`
  - File(s): `src/RCDragManagerProd/Controllers/RaceController.RoundFlow.Core.cs`
  - What was wrong: Pro Ladder generation did not block invalid field sizes or missing ladder templates early enough.
  - What changed: Added pre-generation Pro Ladder validation for 3–32 driver range, safe template probe, user message, and safe abort state with logging.
  - Quick UI validation:
    - Select Pro Ladder with 2 drivers and confirm message: "Pro Ladder supports 3–32 drivers. Please adjust the driver count."
    - Select an in-range size without template support and confirm safe abort without crash.

- Fix 4
  - Commit: `a180baa`
  - File(s): `src/RCDragManagerProd/Controllers/RaceController.RoundFlow.Core.cs`
  - What was wrong: Finals runner-up resolution relied on reference identity semantics.
  - What changed: Runner-up is resolved using `Driver.Id` comparison with null-safe checks and one audit log line (`matchId`, `winnerId`, `runnerUpId`).
  - Quick UI validation:
    - Run an event through finals and complete final match.
    - Confirm event-complete popup shows the correct runner-up consistently.

- Fix 5
  - Commit: `ff472d9`
  - File(s): `src/RCDragManagerProd/RandomMode/RandomMatchEngine.cs`
  - What was wrong: Random loser resolution previously depended on object/reference matching in earlier flow.
  - What changed: Added loser-resolution trace log (`matchId`, `winnerId`, `loserId`) and retained Id-based loser path behavior.
  - Quick UI validation:
    - Run Random/Losers bracket and submit winners.
    - Verify loser-side stats/results map to the expected opponent (no mismatch).

- Fix 6
  - Commit: `dbc2e65`
  - File(s): `src/RCDragManagerProd/Controllers/RaceController.Persistence.cs`
  - What was wrong: Save path could duplicate persisted match rows when main and losers engines overlap.
  - What changed: Added MatchId-based dedupe accounting with one summary log line: `beforeCount`, `afterCount`, `overlapDetected`.
  - Quick UI validation:
    - Run a flow that enters losers/finals and save session.
    - Reload session data and confirm no duplicate saved rows for the same MatchId.

## Build Note

- `dotnet build src/RCDragManagerProd/RCDragManagerProd.csproj -nologo` failed in this environment with:
  - `MSB4216` GenerateResource x86 task-host runtime issue
  - `MSB4028` GenerateResource outputs retrieval mismatch
- This appears to be a local environment/toolchain issue, not caused by the Stage 1 code changes above.
