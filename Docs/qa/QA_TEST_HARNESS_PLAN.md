# QA Test Harness Plan

## 1. Purpose
- This file defines the first practical plan for building an automated test harness for RC Drag Manager.
- The plan is focused on high-value, low-fragility automation that supports safe Codex changes.

## 2. Current Repo Reality
- Application type:
  - Windows desktop WinForms app targeting .NET Framework 4.8 (`src/RCDragManagerProd/RCDragManagerProd.csproj`).
- Core logic shape:
  - Race flow is orchestrated by `RaceController` partials.
  - Mode logic is behind `IRaceEngine` adapters (`ProLadderEngineAdapter`, `RandomEngineAdapter`, `RoundRobinEngineAdapter`).
  - Domain/session state includes `RaceSession`, `MatchResult`, and related models.
- Persistence:
  - SQLite via repositories, especially `RaceSessionRepository` for JSON session save/load.
- Logging:
  - Central logger writes to `%APPDATA%\RC_Drag_Manager\app.log`.
  - Existing marker categories are already documented in `Docs/qa/QA_LOGGING_MARKERS.md`.
- QA documentation baseline exists:
  - `QA_TEST_STRATEGY.md`, `QA_TEST_MATRIX.md`, `QA_TEST_DATA.md`, `QA_LOGGING_MARKERS.md`, `CODEX_CHANGE_AND_TEST_LOOP.md`.
- Current testing reality:
  - No dedicated automated test project is currently present in repo.
  - Full UI automation is not in place and would be fragile for phase 1.

## 3. Harness Goals
- Validate logic after Codex changes.
- Protect core race flow and mode-specific behavior.
- Verify save/load behavior.
- Support deterministic, repeatable checks where possible.
- Avoid dependence on fragile UI automation in phase 1.

## 4. Recommended Phase 1 Harness Design
- Add a dedicated automated test project under `src/` (future implementation).
- Target controller/engine/domain logic first instead of UI control automation.
- Use fixed QA driver packs from `Docs/qa/QA_TEST_DATA.md` as standard inputs.
- Use existing log markers from `Docs/qa/QA_LOGGING_MARKERS.md` as optional verification signals for transition checks.
- Keep WinForms UI validation manual in phase 1.

### Phase 1 design principles
- Prefer direct logic invocation over UI click simulation.
- Keep tests small, scenario-based, and mapped to QA matrix IDs.
- Capture both state outcomes and key transition evidence (state + log where useful).

## 5. Recommended Phase 1 Coverage
Automate these first because they are high-value and mostly logic-driven:
- Pro Ladder pair generation and winner progression.
- BYE handling constraints (no BYE winner selection).
- Random mode structural validation:
  - round/match structure and completion behavior.
  - avoid asserting exact randomized pair identities.
- Round Robin generation and standings/ranking checks.
- Finals/event completion paths:
  - standard RR -> buyback/losers -> finals flow where testable from logic paths.
  - QMDRA finals all-advance path.
- Save/load verification where practical:
  - session save with partial progress and successful reload with expected core state.

## 6. What Stays Manual For Now
- Visual layout and UI rendering checks.
- Button placement and style/presentation validation.
- Next-up display presentation quality (visual UX).
- Manual UX confirmation dialogs and form behavior.
- UI-only flows that are not cleanly exposed through testable logic interfaces.

## 7. Prerequisites Before Implementation
- Confirm canonical build/test commands for this repo.
- Confirm phase-1 test framework choice compatible with the current .NET Framework solution.
- Confirm whether small seams/refactors are needed to improve testability (for example isolating hard UI dependencies from pure logic paths).
- Confirm any additional logging/save-load hooks needed for robust automated verification.
- Confirm whether harness execution should run against root `RCDragManagerProd.sln` or project-local solution files by default.

## 8. Proposed Implementation Sequence
1. Create test project and add it to the chosen solution.
2. Wire a basic test runner and smoke test execution path.
3. Implement first 3 to 5 high-value scenarios from QA matrix:
   - Pro Ladder generation/progression
   - BYE handling
   - Round Robin standard generation
   - save/load round-trip baseline
4. Add state assertions and, where useful, marker-aware checks.
5. Expand to QMDRA and losers/finals scenarios.
6. Map each automated case to QA matrix IDs and QA data pack IDs.
7. Expand coverage iteratively after each stable cycle.

## 9. Risks / Needs confirmation
- Needs confirmation: exact test framework/package choice is not yet defined in repo.
- Needs confirmation: canonical CI/CLI test command set does not yet exist in QA docs.
- Randomized flows may require structural assertions only unless explicit deterministic seeding is introduced.
- Some controller paths are UI-coupled; limited seams/refactors may be needed for clean automation.
- Existing log marker formatting is inconsistent; strict text matching may be brittle without normalization.
