# RC Drag Manager — Race Flow

> **Scope:** the flow below is described against the legacy **WinForms** console
> (`Form1`). The current **WPF** console follows the same controller flow
> (`RaceConsoleView` bound to `RaceConsoleService`); the host window builds the
> controller, and the stats updates happen in `RaceConsoleService` rather than
> `Form1`.

## Step-by-Step: How a Race Event Runs

### Step 1 — App Launch

`Program.cs`:
1. Loads `AppSettings.json` (controls logging).
2. Hooks global exception handlers.
3. Ensures `%APPDATA%\RC_Drag_Manager\` and `race_data.db` exist.
4. Calls `DatabaseInitializer.InitializeDatabase()`.
5. Opens `LandingForm`.

---

### Step 2 — Landing Page

`LandingForm` presents five options:

| Button | Action |
|--------|--------|
| Create Race Session | Opens `MultiClassSetupForm`, then `MultiClassRaceForm` |
| Load Saved Event | Opens `LoadSessionForm` → select a saved event → opens `MultiClassRaceForm` |
| Driver Lists | Opens `DriverManagerForm` |
| Settings | Opens `SettingsForm` |
| Exit | Closes app |

---

### Step 3 — Event Setup (`MultiClassSetupForm`)

The Race Director configures the event. Each class is configured on its own tab (one class per bracket):

1. Enters event name, date, race type (Pro Ladder / Round Robin / Multi-Car Round Robin / Random), and class.
2. For Round Robin: optionally selects QMDRA variant and number of rounds.
3. Selects drivers from the DB roster; sets qualifying times if needed.
4. Clicks "Start Race".

A `RaceSession` object is created and populated with `DriverEntries` (snapshot of selected drivers + cars + dial-ins + seeds). In the WPF flow the event **is** saved to the database before the console opens: `SetupViewModel` calls `MultiClassEventRepository.SaveEvent` when the Director starts the event.

The host opens the console window (`MultiClassRaceForm`, one console per class) and builds a `RaceController` for each class, passing it into the console view.

---

### Step 4 — Race Console (`Form1`)

`Form1` (constructor `Form1(RaceController)`) receives the controller from the host and subscribes to its events. It does not create the controller itself:

| Event | UI Response |
|-------|------------|
| `BracketRedrawn` | Rebuild the pairings ListView |
| `NextMatchReady` | Update the "Next Up" panel and set winner button labels/tags |
| `WinnersUpdated` | Rebuild the winners ListView |
| `CanAdvanceChanged` | Enable/disable "Generate Next Round" button |
| `CanPickWinnerChanged` | Enable/disable winner buttons (currently no UI subscriber, only tests subscribe) |
| `CanOfferBuybackChanged` | Enable "Buy Back" button + show info popup |
| `CanStartFinalsChanged` | Re-enable "Generate Bracket" for finals transition |
| `TournamentCompleted` | Show results popup, update driver stats in DB |

The Race Director clicks **"Generate Bracket"**. This calls `RaceController.GenerateBracket(raceType, drivers)`.

---

### Step 5 — Bracket Generation

`RaceController.GenerateBracket()`:

1. Normalizes `raceType`.
2. Calls `RaceEngineFactory.Create(raceType)` → returns the appropriate `IRaceEngine`.
3. For Round Robin + QMDRA: calls `RoundRobinEngineAdapter.SetRoundsToRun(n)`.
4. Calls `engine.LoadDrivers(drivers)`.
5. Calls `engine.GenerateBracket()`.
6. Reveals rounds: Round Robin formats pre-reveal **every** round (the full schedule is visible from the start, with `_activeRound` set to `roundOrder[0]`), while every other format reveals only the first round label.
7. Fires `BracketRedrawn` and `NextMatchReady`.

---

### Step 6 — Running Matches

For each match in the revealed round:

1. The "Next Up" panel shows the two drivers.
2. The Race Director clicks **"Winner 1"** or **"Winner 2"**.
3. `Form1` calls `controller.SubmitWinner(matchId, firstOption)`.
4. Controller validates (BYE guard, duplicate check), records via `engine.SubmitWinner()` and `_matchResult.SetWinner()`.
5. `WinnersUpdated` fires.
6. `PushNextMatch()` advances the "Next Up" panel to the next unresolved match.
7. `PushAdvanceState()` checks if all visible matches are resolved; enables "Generate Next Round" if so.

A winner can be edited (current round only) via the **"Edit Result"** button → `EditWinnerDialog` → `controller.EditWinnerInActiveRound()`.

---

### Step 7 — Round Advancement

When all matches in the current revealed round are complete:

- "Generate Next Round" becomes enabled.
- Director clicks it → `controller.AdvanceRound()`.
- Round Robin: `_activeRound` moves on to the next round (all rounds are already revealed).
- Non-RR: the next round label is added to `_revealedRounds`.
- `BracketRedrawn` fires with the new set of visible matches.
- Process repeats until no more rounds to reveal.

---

### Step 8 — Phase Transitions (Round Robin path)

After all RR rounds are complete:

1. `PushAdvanceState()` detects that all RR matches are resolved.
2. `RoundRobinRanker` computes standings. A scorecard popup is shown.
3. Eligible buyback drivers are computed (all drivers **not** in top-3).
4. If ≥2 eligible: "Buy Back" button enabled → `BuybackDriverSelectionForm` appears.
5. Director selects which losers to include → `controller.GenerateLosersBracket(selectedDrivers)`.
6. If < 2 eligible: no LB and no auto-advance. The finals gate is raised instead (`FinalsPendingReason = FinalsReasonBuybackSkipped`, a wildcard finalist is chosen, `CanStartFinalsChanged` fires) and the Director must click **"Generate Bracket"** to start the Finals.

**Losers Bracket phase:**
- `LosersBracketBuilder.Build()` creates `List<RandomMatch>` using rematch avoidance against `PairingHistory`.
- A `RandomEngineAdapter` is created and loaded with these matches.
- `_engine` and `_losersEngine` both point to the new adapter.
- `RaceType` on the session becomes `"Losers Bracket"`.
- Rounds proceed as normal (LB-R1, LB-R2, …, LB-F).

**Finals phase:**
- When LB is complete, `_finalsPending = true` and `CanStartFinalsChanged` fires.
- "Generate Bracket" re-enables with a popup saying finals are pending.
- Director clicks → `controller.StartFinals()` → `InjectFinal4Bracket()`.
- Top-3 RR drivers + LB champion form a 4-driver roster.
- A new `ProLadderEngineAdapter` is created and loaded with these 4 drivers.
- `RaceType` becomes `"Finals"`.
- A 4-player Pro Ladder (SF → F) is generated.

**QMDRA path:**
- After `RoundsToRun` rounds are complete and all resolved, **all drivers** advance to finals in RR ranking order.
- All ranked drivers are queued as the finals seeding (`_pendingFinalsRanking`, reason `FinalsReasonRoundRobinAllAdvance`) instead of the buyback flow, and are injected when the Director clicks Start Finals (`InjectFinalsAllAdvance`, `RaceController.RoundFlow.Finals.cs`).
- No LB phase in QMDRA.

---

### Step 9 — Tournament Completion

When the Finals "F" match is resolved:

1. `PushAdvanceState()` detects `session.RaceType == "Finals"` and the final match has a result.
2. Fires `TournamentCompleted` with a `RaceSummary` (winner, runner-up, match count, etc.).
3. `Form1` shows a results popup.
4. Stats updated: `IncrementWinsAndLosses` for each match; `IncrementEventsEntered` for all participants; `IncrementEventsWon` for the champion.

---

## Session Lifecycle Summary

```
New Session
    │
    ├─ MultiClassSetupForm creates RaceSession
    │
    ├─ Console opens → RaceController built by the host
    │
    ├─ GenerateBracket() → engine created + loaded
    │
    ├─ Rounds run: SubmitWinner × N → AdvanceRound × M
    │
    ├─ [Round Robin] → Buyback dialog → LB phase → Finals injection
    │   OR
    ├─ [Pro Ladder / Random] → rounds continue to Final match
    │
    ├─ TournamentCompleted event → stats saved
    │
    └─ User clicks Save → RaceSessionRepository.SaveSession()
                           (INSERT on first save, then UPDATE in place)
```

---

## How Bracket Types Work

### Pro Ladder (NHRA Style)

- `ProLadder.GetLadder(n)` returns a **pre-defined static template** for `n` drivers (3–24, via partial files `L03`–`L24`; any other size returns an empty ladder).
- Templates encode seed matchups for R1 and `FromMatch` references for later rounds.
- Drivers are sorted by qualifying time (fastest = seed 1), then seeded into the template.
- No randomness. The bracket is fully deterministic from the qualifying order.
- `MatchEngine` (legacy) or `ProLadderEngineAdapter` (current) handle match resolution.

### Randomized Single Elimination

- `RandomBracket.GenerateFirstRound()` shuffles all drivers and pairs them sequentially.
- BYE handling: if odd field, the driver who hasn't had a BYE yet (tracked by `byeGiven`) gets the bye slot.
- `RandomBracket.GenerateNextRound(remaining, pairingHistory)` re-shuffles remaining drivers each round, avoiding rematches where possible.
- Entire bracket is **not pre-built** — each round is generated on demand from the current survivors.
- `RandomMatchEngine` stores the current round's matches and results.

### Round Robin

- `RoundRobinEngine.GenerateMatches()` uses the **circle method** (Berger tables) to create all rounds.
- The roster is shuffled before scheduling to avoid predictable BYE assignment.
- An optional pre-rotation further randomizes R1 layout.
- Odd field: null Driver2 = BYE. BYE receiver gets BYE points (2 pts).
- `RoundRobinRanker.Rank()` scores: Win=4, Loss=1, BYE=2. TOTAL = points + 0.1 for each opponent level on points that you beat + 0.001 × the points of every driver you beat; the table is sorted on TOTAL descending.
- Standard mode runs min(3, n-1) rounds. QMDRA mode runs exactly `RoundsToRun` rounds (can exceed n-1, causing deliberate rematches).
- **Multi-Car Round Robin** (multiple cars per driver) does not use the circle method: each round is built by `MultiCarRoundRobinScheduler` at car level, preferring races between cars owned by different drivers.

### Losers Bracket (post-RR)

- `LosersBracketBuilder.Build()` creates a single-elimination bracket for the buyback drivers.
- Pad to next power-of-two with BYE slots.
- Rematch avoidance: before each R1 pairing, check if the pair already appears in `PairingHistory`; if so, try to swap with a later driver.
- Subsequent rounds: winners of each pair advance; odd round sizes carry forward a BYE match.
- Output is `List<RandomMatch>` loaded into a `RandomEngineAdapter`.

### Final-4 (Finals phase)

- Standard RR path: always a Pro Ladder bracket over 3 or 4 drivers.
- QMDRA path: every ranked driver is seeded into a full-size Pro Ladder finals.
- Finalists: Top-3 from RR ranking + 1 LB champion (or 3 drivers if `StartFinalsTop3NoBuyback`).
- Uses `ProLadderEngineAdapter` with a 3- or 4-driver ProLadder template.
- For 4 drivers: SF round (two semis) → Final.
- For 3 drivers: one semi + one bye → Final.

---

## How Results Flow Into Standings and Stats

### In-Event Standings

`RoundRobinRanker.Rank()` is called after each RR round completes (and on final completion). The scorecard is formatted by `RoundRobinScorecardFormatter` and displayed via `ScrollableTextDialog`.

### Persistent Stats

Updated in `Form1` when `TournamentCompleted` fires:

1. **Per-match:** `DriverRepository.IncrementWinsAndLosses(winnerId, loserId)` called for each match result in `summary.MatchResults`.
2. **Events entered:** `DriverRepository.IncrementEventsEntered(driverId)` for each driver in the session roster.
3. **Events won:** `DriverRepository.IncrementEventsWon(winnerId)` for the tournament champion.

`DriverStatsForm` can also recompute `EventsWon` from scratch using `DriverRepository.ComputeEventsWonFromSavedSessions()` — useful if the incremented value is suspected to be stale.
