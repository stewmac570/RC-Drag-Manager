# Live Integration Step 4 Payload Quality Review

## Files Inspected
- `docs/LiveIntegrationReview.md`
- `docs/LiveIntegrationStep2_ControllerReview.md`
- `docs/LiveIntegrationStep3_HooksAndTesting.md`
- `docs/LiveIntegrationSmokeTest.md`
- `src/RCDragManagerProd/Controllers/RaceController.LiveUpdate.cs`
- `src/RCDragManagerProd/Controllers/RaceController.Results.cs`
- `src/RCDragManagerProd/Controllers/RaceController.RoundFlow.Core.cs`
- `src/RCDragManagerProd/Integration/LiveRaceUpdateClient.cs`

## Findings

### 1) `nextUp` correctness
Assessment: **generally correct for current controller truth model**.
- Source logic in `BuildLiveRaceUpdateDto()`:
  - first unresolved match from `EngineGetMatches(_engine)` filtered to `_revealedRounds`
  - same selection model used by `PushNextMatch()` in `RaceController.RoundFlow.Core.cs`
  - lane-adjusted with `GetLaneAdjustedNames(...)`
- Conclusion:
  - `nextUp` should match the controller's active upcoming race.
  - No UI label coupling found.

### 2) `matches` contents
Assessment: **aligned with intended visible bracket rows**.
- Source logic in `BuildLiveRaceUpdateDto()`:
  - uses `BuildCurrentBracketRows()`
  - includes only `!IsHeader`
- This mirrors what the user sees in bracket sections (headers excluded, match rows included).
- Conclusion:
  - Payload contains current visible bracket matches, including visible cross-phase rows as designed.

### 3) round labels for public display
Assessment: **technically valid, possibly not fully user-friendly**.
- `currentRound` uses `GetActiveRoundLabel()` (fallback `_revealedRounds.LastOrDefault()`).
- Labels may be internal-style (`R1`, `LB-R1`, `SF`, `F`).
- Conclusion:
  - Safe and stable for machine/public consumption.
  - If a presentation-friendly label is required later, normalize/format in a future small step.

### 4) event name/date fallbacks
Assessment: **safe with one strict guard**.
- `eventName`: fallback `Quick Session` if blank.
- `eventDate`: requires non-default `DateTime`; otherwise DTO build returns null and send is skipped.
- Conclusion:
  - This avoids sending ambiguous/partial dates.
  - Skip behavior is explicit and logged (`[LIVE][SKIP]`).

### 5) rapid sends / out-of-order risk
Assessment: **was a real risk, now minimally mitigated**.
- Prior behavior:
  - `QueueLiveUpdate` fire-and-forget calls could overlap and complete out of order.
- Small fix implemented (below):
  - serialized `SendAsync` with static `SemaphoreSlim` in `LiveRaceUpdateClient`.
- Result:
  - outbound updates are sent one-at-a-time in invocation order, reducing stale-overwrite risk at server.

## Code Change Made (Minimal, Local)
File changed:
- `src/RCDragManagerProd/Integration/LiveRaceUpdateClient.cs`

Change:
- Added static send gate:
  - `private static readonly SemaphoreSlim SendGate = new SemaphoreSlim(1, 1);`
- Wrapped `SendAsync(...)` body with:
  - `await SendGate.WaitAsync().ConfigureAwait(false);`
  - `finally { SendGate.Release(); }`

Why this is safe:
- No controller/UI flow redesign.
- Caller remains fire-and-forget (`_ = SendAsync(...)`).
- Exceptions still swallowed and logged; no throw-back to race flow.
- Maintains existing payload shape and markers.

## Build/Verification Notes
- Build should be run after this change (see assistant report for status).

## Recommendation: Next Hook Targets (No redesign)
If expanding trigger coverage, safest next controller hooks are:
1. `EditWinnerInActiveRound(...)` success path end
2. `StartLosersBracket()` success path end
3. `InjectFinal4Bracket()` / `InjectFinalsAllAdvance(...)` success path end

Keep same pattern:
- mutate state first
- then `QueueLiveUpdate("<Reason>")` once
- no UI-layer hooks
