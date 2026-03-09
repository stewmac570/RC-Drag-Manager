# QA Test Implementation Guide

## 1. Purpose
- This is the final implementation-planning guide before building the automated test harness for RC Drag Manager.
- It defines the practical bootstrap decisions and initial test scope so the next branch can implement tests with minimal churn.

## 2. Solution Inspection Summary
- Solution files found:
  - `RCDragManagerProd.sln` (repo root)
  - `src/RCDragManagerProd/RCDragManagerProd.sln`
  - `src/RCDragManager.CodeStats/RCDragManager.CodeStats.sln`
- App projects found:
  - `src/RCDragManagerProd/RCDragManagerProd.csproj` (WinForms .NET Framework 4.8)
  - `src/RCDragManager.CodeStats/RCDragManager.CodeStats.csproj`
- Existing test projects found:
  - None tracked in git at this time.
- Relevant source areas confirmed:
  - Controller flow: `src/RCDragManagerProd/Controllers/RaceController*.cs`
  - Race engines/adapters: `src/RCDragManagerProd/RaceEngines/*.cs`
  - Round Robin flow: `src/RCDragManagerProd/RoundRobinMode/*.cs`
  - Session persistence/save-load: `src/RCDragManagerProd/Controllers/RaceController.Persistence.cs`, `src/RCDragManagerProd/Repositories/RaceSessionRepository.cs`
  - Logging usage: `src/RCDragManagerProd/Logging/Logger.cs`, `src/RCDragManagerProd/Config/AppSettings.cs`, plus controller/engine/UI markers documented in `Docs/qa/QA_LOGGING_MARKERS.md`

## 3. Recommended Test Framework and Project Placement
- Recommended framework (phase 1): MSTest v2.
- Why this is the best fit for current repo:
  - Current codebase is classic .NET Framework/Visual Studio solution layout.
  - MSTest integrates cleanly with Visual Studio test discovery for this style.
  - Lowest-friction bootstrap for an initial harness branch.
- Recommended project placement:
  - New test project under `src/`, alongside app projects.
  - Proposed path: `src/RCDragManagerProd.Tests/RCDragManagerProd.Tests.csproj`
- Recommended project name:
  - `RCDragManagerProd.Tests`
- Recommended solution wiring:
  - Add test project to `RCDragManagerProd.sln` (root solution) so QA can run from one top-level solution.
  - Needs confirmation: whether to also include it in `src/RCDragManagerProd/RCDragManagerProd.sln` for parity.

## 4. Recommended First Test Targets
Prioritize high-value logic that is already testable without full UI automation:
- `RaceEngineFactory`
  - mode-to-adapter selection behavior.
- `ProLadderEngineAdapter` + `MatchEngine`
  - bracket generation, winner progression, BYE constraints.
- `RandomEngineAdapter`
  - structural round/match behavior and winner progression paths.
- `RoundRobinEngineAdapter` + `RoundRobinEngine`
  - round generation, BYE handling, standings/top-ranked outputs.
- `RaceController` core flow (targeted methods)
  - bracket generation gating, winner submission rules, round advancement gating.
- `RaceController.Persistence` + `RaceSessionRepository`
  - session save/load behavior and core state restoration expectations.

## 5. Proposed First Automated Scenarios
1. `Factory_SelectsCorrectEngineAdapter`
- Target class/flow: `RaceEngineFactory.Create`
- Why first: simple high-signal guardrail for mode routing.
- Expected assertions (high level): each supported race type returns expected adapter type; unknown type throws.
- Logs/state/output checks: state/assertion only (log check optional).

2. `ProLadder_GeneratesAndAdvancesWinners`
- Target class/flow: `ProLadderEngineAdapter` + `MatchEngine`
- Why first: protects deterministic elimination core.
- Expected assertions: non-empty rounds/matches, winners can be submitted, final champion resolves.
- Logs/state/output checks: state assertions primary; marker checks optional later.

3. `RoundRobin_Standard_GeneratesRoundsAndStandings`
- Target class/flow: `RoundRobinEngineAdapter` / `RoundRobinEngine`
- Why first: covers RR generation and ranking behavior used by multiple flows.
- Expected assertions: matches generated, round labels present, winners settable, standings/top-ranked returned.
- Logs/state/output checks: state assertions primary; optionally verify RR marker presence later.

4. `RoundRobin_QMDRA_RoundCountBehavior`
- Target class/flow: `RoundRobinEngineAdapter.SetRoundsToRun` + RR engine behavior
- Why first: QMDRA is a distinct path with higher regression risk.
- Expected assertions: requested round count respected; overschedule behavior does not crash; rankings still produced.
- Logs/state/output checks: state assertions + optional marker check (`[RR][QMDRA]`) later.

5. `Session_SaveLoad_RoundTripCoreState`
- Target class/flow: `RaceSessionRepository.SaveSession/LoadSession` and controller save data shape
- Why first: persistence regressions are costly and hard to detect visually.
- Expected assertions: session persists and reloads key fields (event metadata, race type, saved results payload presence).
- Logs/state/output checks: state assertions primary; optional DB/session repo marker checks.

## 6. Testability Gaps
- UI coupling:
  - Some flows live in WinForms event handlers and rely on forms/dialog behavior.
- Static/global dependencies:
  - logger/app settings and program-level connection behavior are static.
- Non-deterministic behavior:
  - Random mode and RR shuffle/rotation are intentionally random.
- Logging format inconsistency:
  - marker formats vary (`[TAG]`, plain text, emoji-prefixed lines).
- Save/load access patterns:
  - repository currently uses SQLite connection path/strings and real serialization; test isolation strategy needs confirmation.

## 7. Minimal Refactors Recommended Before or During Harness Bootstrap
- Required before tests:
  - None strictly required to start phase 1 logic tests.
- Optional improvement:
  - Add small helper seams for deterministic randomization in RR/Random engines (injectable RNG wrapper).
  - Centralize canonical test DB path setup helper for repository tests.
- Future cleanup:
  - Normalize logging marker naming for automation parsing.
  - Further separate UI-event code from pure flow logic where practical.

## 8. Proposed Bootstrap Sequence
1. Add new test project `src/RCDragManagerProd.Tests`.
2. Reference `src/RCDragManagerProd/RCDragManagerProd.csproj`.
3. Add shared test data helpers aligned to `Docs/qa/QA_TEST_DATA.md` packs.
4. Implement first smoke tests for 3 to 5 scenarios listed above.
5. Confirm canonical build/test commands and record them in QA docs.
6. Run tests, capture baseline pass/fail status, and map to QA matrix IDs.
7. Expand coverage incrementally (QMDRA/losers/finals/save-load depth) after baseline is stable.

## 9. Merge Recommendation For This Planning Branch
- Merge now.
- Reason: this planning guide is complete, source-backed, and prepares a low-risk bootstrap path without introducing code or project structure changes.

## Needs confirmation
- Confirm final framework/package choice (MSTest v2 vs alternative) before implementation branch starts.
- Confirm canonical solution and command entrypoint for QA runs (`RCDragManagerProd.sln` recommended).
- Confirm deterministic strategy for randomized flows (state-only assertions vs seeded seams).
- Confirm test-database strategy for persistence tests (temporary SQLite file path conventions).
