# Issue Priority — Recommended Order

Recommendation for tackling open issues #159–#167. Ordering balances regression
risk, RD/driver impact, and inter-issue dependencies. Issue numbers and labels
are as filed on `github.com/stewmac570/RC-Drag-Manager`.

---

## Suggested Order

### 1. #159 — BUG-01: Live scoreboard stops updating after RR rounds complete
**Risk:** Low. **Impact:** Very high. **Blocks:** every other live-data work item.

The live broadcast plumbing is already in place — `QueueLiveUpdate` is called
from `RaceController.Results.cs:81` (SubmitWinner) and
`RaceController.RoundFlow.Core.cs:169/220/243` (GenerateBracket, AdvanceRound).
The gap is that neither `StartLosersBracket()`
(`RaceController.RoundFlow.Losers.cs:33`) nor `InjectFinal4Bracket()` /
`StartFinalsTop3NoBuyback()` / `InjectFinalsAllAdvance()`
(`RaceController.RoundFlow.Finals.cs`) call `QueueLiveUpdate` after they swap
the engine and reveal the first round of the new phase. The fix is additive:
one call at the end of each phase-transition method. No existing code path
changes. This is also a **prerequisite** — there is no value pushing dial-ins
(ENH-02), lane data (ENH-01), or richer scorecards (ENH-03) until LB and
Finals reach the server in the first place.

### 2. #162 — BUG-04: Form1 winners bracket column alignment broken
**Risk:** Very low. **Impact:** Medium (RD daily UX). **Blocks:** nothing.

Isolated to `Form1.Display.cs` (`RebuildWinnersView`) and the designer column
widths. No domain logic, no engine paths, no DB. Easy win that improves the
operator's primary view every event. Independent of all other issues.

### 3. #160 — BUG-02: Live scoreboard tabs auto-switch on refresh
**Risk:** Very low (frontend-only). **Impact:** High during multi-class
events. **Blocks:** nothing.

Lives entirely in the stewmacrc.com frontend — does not touch the C# app.
Persisting active tab in localStorage or URL hash is a small, well-understood
pattern. High impact for the multi-class scenario which the app already
supports. Independent of BUG-01 in code, but pairs naturally with it: once
LB/Finals push correctly, the user actually has stable data to look at across
tab refreshes.

### 4. #161 — BUG-03: Live scoreboard classes flicker / change on refresh
**Risk:** Very low. **Impact:** Medium. **Blocks:** nothing.

Frontend-only ordering bug, plus a one-line stabilising sort on the server
side (sort `InMemoryLiveRaceStateStore` keys before serialising). Cheap to
fix once BUG-02 has the team in the live-site frontend already.

### 5. #163 — BUG-05: Single buyback driver forces LB run instead of going direct to finals
**Risk:** Medium. **Impact:** Medium (event-blocking when it occurs).
**Blocks:** nothing.

The override mechanism already exists: `_buybackChampionOverride` in
`RaceController.RoundFlow.Finals.cs:32-36` is a documented wildcard path that
sends a single driver straight into Finals without an LB engine. The fix is
not "build a new path" but "route the single-driver case into the existing
override path." `BuybackDriverSelectionForm.cs:31-36` already accepts
`>=1` selections — the rejection is at `GenerateLosersBracket` /
`StartLosersBracket`, which both gate on `< 2`. Lift those gates for the
single-driver case, set `_buybackChampionOverride`, and call
`InjectFinal4Bracket()` directly. Medium risk only because it touches the
LB→Finals control flow; testable against existing event scenarios.

### 6. #164 — ENH-01: Show lane assignments (Left/Right) on live scoreboard
**Risk:** Low. **Impact:** Medium-high. **Depends on:** BUG-01.

Additive change: one new field on `LiveMatchDto`, populated in
`RaceController.LiveUpdate.cs:74-82` from the existing `LaneFairnessManager`
(`GetLaneAdjustedNames` is already called for `nextUp` — extend the
projection). Server passes through; frontend renders side-by-side. Why after
BUG-01: there is no point sending lane data for LB/Finals matches if those
matches never reach the server.

### 7. #166 — ENH-03: Round Robin scoring transparency — detailed scorecard
**Risk:** Low. **Impact:** Medium (driver-facing). **Depends on:** BUG-01
(for the live-site portion).

`RoundRobinScorecardLogger.BuildScorecard` is already piped into
`LiveRaceUpdateDto.RRStandings` (`RaceController.LiveUpdate.cs:95-101`) and
into the post-round popup. Extension is additive — expose more detail from
`RoundRobinRanker.Rank()` and grow the formatter output. No risk to existing
RR scoring math because the scoring/tiebreaker logic itself is unchanged;
only the *display* is being expanded.

### 8. #165 — ENH-02: Driver dial-in display and self-update on live scoreboard
**Risk:** High. **Impact:** Very high (most-requested driver feature).
**Depends on:** BUG-01 and arguably ENH-04.

Largest of the enhancements: new server endpoint with PIN storage, frontend
update form, app-side polling to read updates back into `DriverEntries`,
Form1 winner-button display, plus a "lock on Generate Next Round" gate. The
server-write-back-into-the-app path is new ground for this codebase — every
existing live integration is push-only. Also produces concurrency questions
(driver edits dial-in mid-round, RD edits at the same time, who wins). Worth
doing because drivers want it, but worth doing carefully and after the
foundational fixes land. **If ENH-04 is going to happen, it should land
before ENH-02** so the new endpoint is event-scoped from day one and does
not need a follow-up migration.

### 9. #167 — ENH-04: Multi-event support on stewmacrc.com
**Risk:** High (architectural). **Impact:** Low today (only matters with
concurrent RDs); high once that situation arises. **Depends on:** BUG-01.

Touches `RaceSession`, every live DTO, every server endpoint, the server
store, and the frontend landing page. Doing this last after BUG-01 means the
core push path is already correct; doing it before ENH-01/03 means those
features ship event-scoped from the start. Recommend slotting **immediately
before ENH-02** if the team is committed to ENH-02 — otherwise leaving it
last is fine, with the understanding that ENH-01/03 will need a small
migration when ENH-04 eventually lands.

---

## Dependency Summary

```
BUG-01 ──┬── ENH-01
         ├── ENH-03
         └── ENH-04 ── ENH-02

BUG-04   (independent)
BUG-02   (independent, frontend-only)
BUG-03   (independent, frontend-only)
BUG-05   (independent)
```

`BUG-01` is the only hard prerequisite for the enhancements — until LB and
Finals data reach the server, every richer payload is wasted on those
phases. `ENH-04` is a soft prerequisite for `ENH-02`: if both are planned,
do `ENH-04` first to avoid a second round of endpoint rework.

---

## Recommended First Issue

**Start with #159 (BUG-01).** It is the lowest-risk fix in the list — purely
additive `QueueLiveUpdate(...)` calls at three known phase-transition
points — and it unblocks every live-feed enhancement. Without it, any work
on ENH-01/02/03/04 is partly invisible to drivers because LB and Finals
phases never make it to the live site.
