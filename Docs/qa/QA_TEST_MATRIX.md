# QA Test Matrix

## 1. Purpose
- This matrix is the working list of test scenarios for current manual QA and future automated QA.
- It tracks what must be validated for race outcome safety before merge.

## 2. Test Matrix Table

| ID | Feature Area | Scenario | Setup / Input | Expected Result | Verification Method | Status | Notes |
|---|---|---|---|---|---|---|---|
| QA-001 | Pro Ladder | Pro Ladder bracket generation for valid roster | Create session with `RaceType=Pro Ladder`, 8 drivers with qualifying times | `ProLadderEngineAdapter` created and initial round appears with valid pairings | Manual UI check + Log check | Pending | Confirmed by `RaceEngineFactory` and controller generation flow |
| QA-002 | Pro Ladder | Pro Ladder winner progression across rounds | Run Pro Ladder session and submit winners through all visible rounds | Winners propagate to later matches and tournament can complete | Manual UI check + Code-path review | Pending | Focus on downstream `FromMatch` dependency behavior |
| QA-003 | Pro Ladder / BYE | BYE handling in ladder mode | Use odd-sized valid Pro Ladder roster (for example 3 or 5 drivers) and progress round | Real driver auto-advances from BYE path; BYE cannot be selected as winner | Manual UI check + Log check | Pending | `ByePolicy.IsBye` is null-based; verify UI mapping in winner buttons |
| QA-004 | Random Mode | Random bracket generation | Create session with `RaceType=Random`, 8+ drivers | `RandomEngineAdapter` created, matches generated, rounds ordered | Manual UI check + Log check | Pending | Check `[ENGINE FACTORY]` and adapter logs |
| QA-005 | Random Mode | Random winner progression and completion | Submit winners through Random rounds until final | Winners advance correctly and champion is resolvable | Manual UI check + Code-path review | Pending | Include at least one run with odd count to exercise BYE recipient logic |
| QA-006 | Round Robin | Round Robin round generation (Standard) | Create `RaceType=Round Robin`, `RoundRobinVariant=Standard`, 6 drivers | RR matches generated with expected round labels and first round revealed | Manual UI check + Log check | Pending | Verify `SetRoundsToRun(3)` path |
| QA-007 | Round Robin | No duplicate pairings within expected limits | Run Standard RR with driver count where unique pairings are possible and review pairings | No duplicate head-to-head pairings within first full RR cycle | Log check + Code-path review | Pending | Needs confirmation: for QMDRA overschedule, rematches are expected after max unique rounds |
| QA-008 | Round Robin | Standings/rankings generation | Complete RR rounds and open standings flow | Standings available and ranking order produced from RR engine/ranker | Manual UI check + Log check | Pending | Verify scorecard log output and standings popup behavior |
| QA-009 | QMDRA | QMDRA completion path (no buyback) | Create RR session with `RoundRobinVariant=QMDRA`, set `RoundsToRun` > 0, complete N rounds | Controller advances all ranked drivers to finals seed order via QMDRA finals injection | Manual UI check + Log check | Pending | Verify `[RR][QMDRA]` and `[FINALS][QMDRA]` markers |
| QA-010 | Buyback / Losers | QMDRA buyback path gating | Run QMDRA RR to completion and inspect buyback/losers options | Buyback path should not be required for QMDRA completion route | Manual UI check + Code-path review | Pending | Needs confirmation: validate exact UI button state expectations in current build |
| QA-011 | Losers Bracket | Losers bracket generation from selected drivers | In Standard RR completion flow, select eligible buyback drivers and start losers bracket | LB engine created, `LB-R1` revealed, first LB match ready | Manual UI check + Log check | Pending | Verify `GenerateLosersBracket` -> `StartLosersBracket` flow |
| QA-012 | Finals / Completion | Finals path and event completion | Complete RR + LB path, then start finals and submit final winners | Final bracket injects and event completion summary is emitted | Manual UI check + Log check | Pending | Verify `TournamentCompleted` event and completion message |
| QA-013 | Persistence | Session save/load restore | Save active session with partial results, reload from Load Session form | Restored session retains race type, saved results, and revealed rounds behavior | Save/load verification + Manual UI check | Pending | Needs confirmation: verify exact extent of UI state restoration on load |
| QA-014 | UI Winner Flow | Winner selection UI mapping and safeguards | Use winner buttons on normal and lane-swapped matches; include BYE match | Correct engine-side winner chosen, BYE cannot be manually selected as winner | Manual UI check + Log check | Pending | Check for `[UI][WINNER][MAP-WARN]` and rejection markers |
| QA-015 | Round Control | Round advancement gating | Attempt `Next Round` before all visible matches are resolved, then after resolution | Button remains disabled until visible matches resolved; enables when eligible | Manual UI check + Log check | Pending | Verify `PushAdvanceState` logic with visible vs unresolved counts |
| QA-016 | Logging / Diagnostics | Key logging markers present for critical actions | Run end-to-end scenario across modes and inspect `app.log` | Expected markers exist for engine selection, winner submission/reject, advancement, save/load, and finals | Log check + Needs automation later | Pending | Build `QA_LOGGING_MARKERS.md` next to formalize exact marker list |

## Needs confirmation
- QMDRA explicitly bypasses buyback in current controller logic; UI gating expectations for buyback controls in QMDRA runs should be confirmed in-app.
- Save/load currently persists session JSON and controller save data, but exact UI rehydration depth per screen should be confirmed through manual run-through.
- Some higher-level docs in `Docs/` may not fully match current implementation; this matrix is based on current source paths under `src/RCDragManagerProd`.
