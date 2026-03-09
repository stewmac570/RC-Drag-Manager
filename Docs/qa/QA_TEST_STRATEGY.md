# QA Test Strategy

## 1. Purpose
- The QA process exists to reduce regressions in race outcome behavior (pairings, winners, round gating, finals progression, and persisted state).
- This strategy establishes a repeatable Codex loop: code change -> build -> targeted test run -> log verification -> merge decision.
- The immediate goal is to make each future Codex run verify race-critical behavior before merge.

## 2. Project Testing Goals
- Protect core race logic from regressions across `ProLadderEngineAdapter`, `RandomEngineAdapter`, and `RoundRobinEngineAdapter`.
- Protect bracket correctness, including seed mapping, round labels, and upstream match propagation.
- Protect round progression and gating (`CanAdvanceChanged`, revealed rounds, visible-round completion checks).
- Protect persistence/save-load behavior (`RaceController.SaveSession()`, `RaceSessionRepository.SaveSession()/LoadSession()`).
- Protect UI behavior that affects race outcomes (winner button mapping, BYE handling, lane swap mapping).
- Support future automated validation by defining deterministic scenarios and expected outcomes now.

## 3. Core Feature Areas To Test
- Pro Ladder mode
- Random mode
- Round Robin mode
- QMDRA / buyback / losers bracket / finals flow
- Session save/load
- Driver and race state management
- Winner selection flow
- BYE handling
- Round advancement
- Standings / rankings where applicable
- Logging and diagnostics

### What is currently confirmed in code
- `RaceEngineFactory` selects `ProLadderEngineAdapter`, `RandomEngineAdapter`, or `RoundRobinEngineAdapter` by race-type string.
- Round Robin supports `Standard` and `QMDRA` variants (`RaceSession.RoundRobinVariant`, `RoundsToRun`, `SetRoundsToRun`).
- Buyback/Losers flow exists: eligible drivers -> `LosersBracketBuilder` -> `RandomEngineAdapter` losers engine -> finals injection.
- Finals flow exists: `InjectFinal4Bracket()`, `StartFinals()`, and QMDRA all-advance finals path.
- Winner entry is UI-driven and mapped through lane-swap logic before `RaceController.SubmitWinner()`.
- BYE-as-winner is blocked and BYE auto-advance paths are logged.
- Save/load uses JSON session persistence through `RaceSessions.SessionData`.

## 4. Risk Areas
- Winner propagation from one round to downstream matches (`FromMatch1`/`FromMatch2` dependencies).
- Match result recording consistency between engine state and `_matchResult` cache.
- Round completion gating (`PushAdvanceState`) and next-round reveal correctness.
- BYE auto-advancement fairness and invalid BYE winner prevention.
- Session restore consistency after save/load (results, revealed rounds, race type, and driver state).
- UI/controller sync during winner selection, especially lane-swapped matches.
- Engine/controller boundary issues (controller wrappers calling engine methods, round-label normalization).
- Changes that affect multiple race modes via shared interfaces (`IRaceEngine`, `RaceController` partial flow files).

## 5. Testing Layers
- Document review
- Manual smoke testing
- Scenario-based functional testing
- Regression testing
- Log verification
- Future unit/integration testing

### Layer intent for this repo now
- Document review: confirm expected behavior against `src/RCDragManagerProd` implementation before changing logic.
- Manual smoke testing: run one short session per race mode and verify basic end-to-end progression.
- Scenario-based functional testing: run fixed datasets for RR standard, RR QMDRA, random bracket, and pro ladder.
- Regression testing: replay known critical scenarios after each race-flow/controller change.
- Log verification: confirm key markers for engine selection, winner submission, round transitions, save/load, and finals injection.
- Future unit/integration: prioritize engine-only deterministic tests first, then controller integration tests around round gating and persistence.

## 6. Initial Test Principles
- Use fixed known test data.
- Require deterministic expected outcomes for each scenario.
- No guessing: every pass/fail decision must map to expected UI state and/or log markers.
- Isolate one feature change at a time.
- Always verify logs for important transitions.
- Manual verification is required for UI-visible race behavior until automated UI testing exists.

### Additional practical rules
- Validate both data result and UI effect for winner selection.
- When a change touches shared interfaces or controller flow, run all three race modes.
- Treat save/load as mandatory regression coverage for any round-flow, finals, or result-recording change.

## 7. Recommended First Test Assets
Create these next documents in `docs/qa/`:
- `QA_TEST_MATRIX.md`
- `QA_TEST_DATA.md`
- `QA_LOGGING_MARKERS.md`
- `CODEX_CHANGE_AND_TEST_LOOP.md`

## 8. Exit Criteria For A Safe Change
A change is safe to merge when all of the following are true:
- Build succeeds for `src/RCDragManagerProd`.
- A documented smoke run is completed for affected mode(s), and for all modes when shared flow is changed.
- Critical scenarios for winner recording, round advancement, and BYE handling pass with expected outcomes.
- Save/load round-trip is verified for affected flow (including restored winners and revealed rounds).
- Required log markers are present for engine selection, winner set/reject, advancement gating, and finals transitions.
- No new race-outcome-impacting UI/controller mismatches are observed.
- Any unresolved uncertainty is listed explicitly as `Needs confirmation` before merge.

## Needs confirmation
- `Docs/PROJECT_STATUS.md` appears outdated versus current implementation (it lists Random/Round Robin/save-load as partial/not built, but code includes active implementations).
- Several architecture docs under `Docs/` describe components (for example `MatchRepository`, extended logging/reports) that are not present as concrete classes in `src/RCDragManagerProd`.
- UI surface docs may include controls/flows not exactly matching current `Form1` implementation; test assets should be based on source code behavior first.
