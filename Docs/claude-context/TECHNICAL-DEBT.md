# RC Drag Manager — Technical Debt

## GitHub Issues (#94–#105)

All 12 issues raised in March 2026 have been closed and merged.

| # | Title | Type | Status | PR / Fix |
|---|-------|------|--------|---------|
| #94 | `TotalWins` and `TotalLosses` are never written — stat columns always stay at 0 | bug | ✅ Closed | Fixed by wiring `IncrementWinsAndLosses` into `TournamentCompleted` handler |
| #95 | `RaceController.Reset()` leaves stale `_matchResult` and RR snapshots, corrupting next session | bug | ✅ Closed | `Reset()` now clears all state: `_matchResult`, `_rrTop3`, `_rrMatchesSnapshot`, `_rrRoundOrderSnapshot`, `_rrStandingsCardCache`, `_rrLoggedRounds` |
| #96 | `RaceSession.PairingHistory` uses `HashSet<(int,int)>` which `System.Text.Json` cannot serialize | bug | ✅ Closed | Added `PairingHistoryRaw = List<int[]>` as backing store; `PairingHistory` marked `[JsonIgnore]` |
| #97 | `RoundRobinEngine.GetTopRankedDrivers` throws `InvalidOperationException` on driver ID mismatch | bug | ✅ Closed | Fixed ranker to handle missing driver IDs gracefully |
| #98 | `RoundRobinEngine.GetStandings` groups winners by object reference, not by driver ID | bug | ✅ Closed | Changed grouping key from object reference to `d.Id` |
| #99 | Standard Round Robin hardcodes 3 rounds regardless of driver count | bug | ✅ Closed | Standard RR now runs `min(3, n-1)` rounds, clamped to available unique rounds |
| #100 | `InjectFinal4Bracket` hardcodes `'SF'` as the first revealed round — breaks 3-finalist finals | bug | ✅ Closed | Replaced hardcode with `EngineGetRoundOrder(_engine).FirstOrDefault()` |
| #101 | `ExecuteStatIncrement` interpolates column name directly into SQL — unsafe pattern | enhancement | ✅ Closed | Added `_allowedStatColumns` whitelist; throws `ArgumentException` for unknown column names |
| #102 | `RaceSessionRepository.SaveSession` takes `object` and uses reflection — type safety lost | enhancement | ✅ Closed | Changed signature to `SaveSession(RaceSession session)`; removed reflection helpers |
| #103 | `LosersBracketBuilder.Norm` is dead code — history parameter never actually filters pairings | enhancement | ✅ Closed | `Norm` method and `history` parameter now wired into R1 pairing loop with rematch avoidance |
| #104 | `RoundRobinRanker.Rank` throws `KeyNotFoundException` when winner/loser ID not in stats dictionary | bug | ✅ Closed | Added `ContainsKey` guards before accessing stats dictionary |
| #105 | `OnTournamentCompleted` bumps `EventsEntered` using `Form1`'s local driver list, not DB-persisted IDs | bug | ✅ Closed | Changed to use `DriverEntry.DriverID` values from the session's persisted entries |

---

## Known Architectural Weaknesses

### 1. Session Save is Always INSERT (No Update)

`RaceSessionRepository.SaveSession` always does an INSERT. There is no UPDATE path. Saving an in-progress session multiple times creates multiple rows. The user must manually pick the correct (latest) row when resuming. Old rows accumulate indefinitely.

**Impact:** Minor UX inconvenience; no data corruption. Fix would require an UPDATE path and a way to identify the "canonical" save for a session.

---

### 2. Bracket State Not Persisted — Sessions Don't Resume Mid-Bracket

When a session is loaded from the database, the `RaceSession` JSON is deserialized but the **engine state is not rebuilt**. The bracket (match tree, driver positions, seeds) is not stored — only the scalar match results (`SavedResults`) and revealed round labels are. Resuming a session effectively means restarting from scratch: the Race Director must regenerate the bracket and re-enter results.

**Impact:** Significant for mid-event saves. Workaround: don't save until the event is done, or manually reconstruct.

---

### 3. Two Overlapping Car Access Paths

Both `DriverRepository` and `CarRepository` handle car records. `DriverRepository` does it more completely (create/update/delete with transactions), while `CarRepository` is a lightweight subset. Some forms use one, some use the other. There is no single authoritative car repository.

**Impact:** Maintenance overhead. Fix: consolidate all car access into `DriverRepository` and retire `CarRepository`.

---

### 4. `MatchEngine` (Legacy) and Adapters Coexist

`MatchEngine.cs` is the original Pro Ladder engine predating the `IRaceEngine` abstraction. It is still referenced by some code paths. `ProLadderEngineAdapter` wraps it and is the correct current path. Having both creates confusion about which to use.

**Impact:** Risk of divergent behavior if `MatchEngine` is accidentally used directly. Fix: deprecate direct `MatchEngine` use; route everything through adapters.

---

### 5. `RaceController` Has Significant Length and Complexity

The controller is split across 11 partial files. While the split helps navigate individual concerns, the total size and the number of distinct responsibilities (session lifecycle, RR standings, LB flow, finals injection, live feed, persistence) make it a complex class to reason about as a whole.

**Impact:** High cognitive load for new developers. Future refactor could extract the phase-transition logic (RR→LB→Finals) into a dedicated state machine.

---

### 6. `Form1` Still Contains Some Business Logic

Despite the controller layer, `Form1` still does some non-trivial things: it calls `DriverRepository.IncrementWinsAndLosses` directly when processing `TournamentCompleted`, and it constructs `RaceSessionDriverEntry` objects during session save. These should ideally live in the controller.

**Impact:** Violation of the "no direct DB access from UI" rule. Minor but worth cleaning up.

---

### 7. No Session Update / Resume Architecture

Related to point 2: the architecture has no concept of "resume session". `LoadSession` deserializes the JSON and hands the `RaceSession` object to `Form1`, but `Form1` then calls `GenerateBracket` fresh. The `SavedResults` and `SavedRevealedRounds` fields exist on `RaceSession` but there is no code path that uses them to reconstruct the engine from a saved state.

**Impact:** Sessions cannot be practically resumed mid-event.

---

### 8. `RandomBracket.byeGiven` is Static Mutable State

`RandomBracket.byeGiven` is a `static readonly HashSet<int>` tracking who received a BYE. This is shared across all instances and sessions in the same app process. `ResetByeTracker()` must be called at the start of each new event. If missed, BYE tracking leaks across sessions.

**Impact:** Bug risk if a new session is started without calling `ResetByeTracker()`. The `Random` instance in `RandomBracket` is also static.

---

### 9. Live Feed Integration Is Partially Wired

`RaceController.LiveUpdate.cs` and `Integration/LiveApiClient.cs` implement an optional HTTP live feed push (sends current bracket state to a local server). It is gated by `AppSettings.LiveBroadcastEnabled` (a JSON-persisted setting managed via the Settings dialog). The integration exists but is not fully documented or formally part of the core flow. The spec is in `Docs/Live Feed Refresh Behaviour spec.md`.

**Impact:** If enabled accidentally it will attempt HTTP calls during every match resolution.

---

## Areas Flagged for Future Improvement

From the original `_PROJECT_STATUS_SUMMARY.md` Phase 7 plan:

1. **Race Results Export** — CSV/PDF export for event summaries and driver stats.
2. **Session History Viewer** — sortable, filterable table of past events.
3. **Online Sync (optional)** — cloud backup of driver stats and session history.
4. **UI Themes** — dark/light modes.
5. **Performance Profiling** — especially for large driver registries.
6. **Session Resume** — proper bracket reconstruction from saved state.
7. **CarRepository consolidation** — retire `CarRepository`; all car logic in `DriverRepository`.
8. **ProLadder extended to 32** — currently only up to L24 (24 drivers) has a tested template. Files L25–L32 may not exist or may need validation.
