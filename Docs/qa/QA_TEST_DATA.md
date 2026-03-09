# QA Test Data Definitions

## 1. Purpose
- This file defines fixed, reusable QA test data packs for RC Drag Manager.
- These packs are the baseline inputs for repeatable manual testing now and future automated testing later.

## 2. Test Data Principles
- Use fixed driver names so logs and outcomes can be compared run-to-run.
- Use fixed qualifying times where seed order matters (especially Pro Ladder).
- Keep setups deterministic where the code allows deterministic behavior.
- Reuse the same packs across smoke, scenario, and regression runs.
- Keep packs small enough for practical manual verification.
- Do not assume behavior that is not implemented in current repo code.

## 3. Standard Driver Packs

### TD-001: Pro Ladder 4-Driver Pack
- Intended feature area: Pro Ladder bracket generation and winner propagation.
- Driver list:
  - Ava Stone (QualTime: 3.910)
  - Blake Turner (QualTime: 3.955)
  - Casey Reed (QualTime: 4.005)
  - Drew Cole (QualTime: 4.080)
- Seed/qualifying assumptions:
  - Pro Ladder engine orders by timed first, fastest-to-slowest, then name tie-breaker.
  - Expected seed order: Ava, Blake, Casey, Drew.
- Why this pack exists:
  - Minimal valid Pro Ladder set with no BYE complexity.
- Notes / Needs confirmation:
  - Needs confirmation: exact first-round visual matchup ordering should be captured from app UI once and reused as baseline.

### TD-002: Round Robin 5-Driver Odd Pack
- Intended feature area: Round Robin odd-count BYE handling, round progression, duplicate-pair checks.
- Driver list:
  - Ava Stone
  - Blake Turner
  - Casey Reed
  - Drew Cole
  - Evan Hart
- Seed/qualifying assumptions:
  - Round Robin does not rely on seed; roster is shuffled internally before pairing.
  - BYE appears because driver count is odd.
- Why this pack exists:
  - Exercises RR BYE assignment and per-round BYE distribution logs.
- Notes / Needs confirmation:
  - Needs confirmation: exact per-round BYE recipient is intentionally not deterministic due shuffle/random pre-rotation.

### TD-003: Round Robin 6-Driver Even Pack
- Intended feature area: Standard RR generation, standings/ranking, no-BYE baseline.
- Driver list:
  - Ava Stone
  - Blake Turner
  - Casey Reed
  - Drew Cole
  - Evan Hart
  - Flynn Ward
- Seed/qualifying assumptions:
  - Round Robin standard mode runs 3 rounds through controller setup.
  - No BYE expected with even count.
- Why this pack exists:
  - Clean RR functional baseline for standings and duplicate pairing checks.
- Notes / Needs confirmation:
  - Needs confirmation: duplicate-pair assertions should be limited to first full unique cycle; QMDRA overschedule can introduce rematches.

### TD-004: Random Mode 8-Driver Pack
- Intended feature area: Random bracket generation, winner progression, final resolution.
- Driver list:
  - Ava Stone
  - Blake Turner
  - Casey Reed
  - Drew Cole
  - Evan Hart
  - Flynn Ward
  - Gray Nash
  - Harper Quinn
- Seed/qualifying assumptions:
  - Random mode pairing is not fixed; verify structural outcomes, not specific pair identities.
- Why this pack exists:
  - Provides stable roster size for repeated Random mode regressions without BYE.
- Notes / Needs confirmation:
  - Needs confirmation: if deterministic Random runs are needed later, code support for explicit RNG seed control is not currently exposed.

### TD-005: Save/Load Mid-Event Pack
- Intended feature area: Session persistence, revealed-round state, match result restore.
- Driver list:
  - Use TD-003 (RR 6-driver even) as base.
- Seed/qualifying assumptions:
  - Save after at least one full visible round is resolved and before event completion.
- Why this pack exists:
  - Validates `RaceController.SaveSession()` + `RaceSessionRepository.SaveSession()/LoadSession()` behavior.
- Notes / Needs confirmation:
  - Needs confirmation: exact UI reconstruction depth after load should be verified manually (not all state is explicitly documented).

### TD-006: QMDRA / Buyback / Finals Flow Pack
- Intended feature area: RR QMDRA path, Standard RR buyback path, losers bracket, finals completion.
- Driver list:
  - Ava Stone
  - Blake Turner
  - Casey Reed
  - Drew Cole
  - Evan Hart
  - Flynn Ward
  - Gray Nash
- Seed/qualifying assumptions:
  - QMDRA run: `RoundRobinVariant=QMDRA`, `RoundsToRun` > 0.
  - Standard RR run: `RoundRobinVariant=Standard` for buyback/losers bracket flow.
- Why this pack exists:
  - Reuses one roster to test both branches:
  - QMDRA all-advance finals path (no buyback dependency).
  - Standard RR top3 + buyback selection -> losers bracket -> finals injection.
- Notes / Needs confirmation:
  - Needs confirmation: QMDRA buyback UI gating behavior should be validated in-app; controller logic indicates QMDRA completion path bypasses buyback.

## 4. Event Setup Definitions

### Setup Template A: Pro Ladder Baseline
- Race mode: Pro Ladder.
- Class/event type: Heads Up preferred for simplest qualifying-time path.
- Starting round expectations:
  - First round is revealed immediately after bracket generation.
- BYE expectations:
  - None when using TD-001; BYE possible with odd-sized Pro Ladder sets.
- Winner-selection expectations:
  - Winner buttons submit through controller mapping and reject BYE-as-winner.
- Save/load checkpoint suggestion:
  - Save after first-round winners entered, before final round.
- Expected log coverage areas:
  - `[ENGINE FACTORY]`, `[ENGINE]`, `[WINNER]`, `[DEBUG] PushAdvanceState`, `[SAVE]`.

### Setup Template B: Round Robin Standard Baseline
- Race mode: Round Robin (`Standard`).
- Class/event type: Heads Up preferred.
- Starting round expectations:
  - Round `RR1` revealed first.
  - Controller sets RR rounds to 3.
- BYE expectations:
  - TD-002 (odd) includes BYE; TD-003 (even) has no BYE.
- Winner-selection expectations:
  - Manual winner entry per match; round advance only after visible matches resolved.
- Save/load checkpoint suggestion:
  - Save at end of RR2 and load to confirm continuation into RR3.
- Expected log coverage areas:
  - `[RR] Build`, `[RR][BYE]` (odd only), `[RR-SCORE]`, `[ROUND ROBIN] Final standings`, `[SAVE]`.

### Setup Template C: Round Robin QMDRA Baseline
- Race mode: Round Robin (`QMDRA`, `RoundsToRun=N`).
- Class/event type: Heads Up preferred.
- Starting round expectations:
  - RR first round revealed; completion after N revealed rounds resolved.
- BYE expectations:
  - If odd roster, BYE entries appear in RR rounds.
- Winner-selection expectations:
  - Same winner flow as RR standard.
- Save/load checkpoint suggestion:
  - Save at N-1 rounds complete, then load and finish N.
- Expected log coverage areas:
  - `[RR][QMDRA]`, `[FINALS][QMDRA]`, finals seed-order logs, `[SAVE]`.

### Setup Template D: Standard RR + Buyback + Finals Baseline
- Race mode: Round Robin (`Standard`) followed by buyback/LB/finals flow.
- Class/event type: Heads Up preferred.
- Starting round expectations:
  - RR rounds complete first, then buyback selection for eligible drivers.
- BYE expectations:
  - Depends on roster parity and losers bracket composition.
- Winner-selection expectations:
  - Winners entered manually in RR, LB, and finals phases.
- Save/load checkpoint suggestion:
  - Save once LB is generated (`LB-R1` revealed), load and finish to event completion.
- Expected log coverage areas:
  - `GenerateLosersBracket`, `StartLosersBracket`, `LB-R1` reveal logs, `[FINALS]`, `TournamentCompleted` UI log.

## 5. Usage Guidance
- Manual smoke testing:
  - Use TD-001, TD-003, and TD-004 for quick per-mode confidence checks.
- Scenario validation:
  - Use TD-002 for odd-count RR BYE scenarios.
  - Use TD-006 with Setup C and D to validate QMDRA and buyback/finals branches.
- Regression testing:
  - Re-run the same pack/setup combinations after any controller, engine, winner-flow, or persistence change.
- Future Codex-driven test workflows:
  - Reference pack IDs directly in future QA docs and prompts.
  - Keep expected results structural unless deterministic RNG controls are added.
  - Expand with machine-checkable assertions once unit/integration tests are introduced.

## Needs confirmation
- Exact deterministic replay controls for random pairing/lane assignment are not exposed as user-configurable test seeds.
- Full UI state restoration behavior after load should be validated and then documented as explicit pass/fail checks.
- Some legacy docs may diverge from current code; this file prioritizes source-verified behavior in `src/RCDragManagerProd`.
