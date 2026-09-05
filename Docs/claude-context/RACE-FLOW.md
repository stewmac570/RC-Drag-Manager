# RC Drag Manager — Race Flow

## Step-by-Step: How a Race Event Runs

### Step 1 — App Launch

`Program.cs`:
1. Loads `AppSettings.json` (controls logging).
2. Hooks global exception handlers.
3. Ensures `%APPDATA%\RC_Drag_Manager\` and `race_data.db` exist.
4. Calls `DatabaseInitializer.InitializeDatabase()`.
5. Opens `LandingPageForm`.

---

### Step 2 — Landing Page

`LandingPageForm` presents four options:

| Button | Action |
|--------|--------|
| New Event | Opens `SessionSetupForm` |
| Load Event | Opens `LoadSessionForm` → select saved session → opens `Form1` |
| Manage Drivers | Opens `DriverManagerForm` |
| Exit | Closes app |

---

### Step 3 — Session Setup (`SessionSetupForm`)

The Race Director configures the event:

1. Enters event name, date, race type (Pro Ladder / Round Robin / Random), and class.
2. For Round Robin: optionally selects QMDRA variant and number of rounds.
3. Selects drivers from the DB roster; sets qualifying times if needed.
4. Clicks "Start Race".

A `RaceSession` object is created and populated with `DriverEntries` (snapshot of selected drivers + cars + dial-ins + seeds). The session is **not yet saved to the database** at this point.

`Form1` is opened with the session object and connection string.

---

### Step 4 — Race Console (`Form1`)

`Form1` creates a `RaceController(session)` and subscribes to its events:

| Event | UI Response |
|-------|------------|
| `BracketRedrawn` | Rebuild the pairings ListView |
| `NextMatchReady` | Update the "Next Up" panel and set winner button labels/tags |
| `WinnersUpdated` | Rebuild the winners ListView |
| `CanAdvanceChanged` | Enable/disable "Generate Next Round" button |
| `CanPickWinnerChanged` | Enable/disable winner buttons |
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
6. Gets the first round label and adds it to `_revealedRounds`.
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
- Next round label is added to `_revealedRounds`.
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
6. If < 2 eligible: auto-advance with wildcard, no LB.

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
- `InjectFinalsAllAdvance(rankedDrivers)` fires instead of the buyback flow.
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
    ├─ SessionSetupForm creates RaceSession
    │
    ├─ Form1 opens → RaceController created
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
                           (new INSERT every time)
```

---

## How Bracket Types Work

### Pro Ladder (NHRA Style)

- `ProLadder.GetLadder(n)` returns a **pre-defined static template** for `n` drivers (3–24, extended to 32 via partial files `L03`–`L24`).
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
- `RoundRobinRanker.Rank()` scores: Win=4, Loss=1, BYE=2. Tiebreakers: head-to-head → opponent score (the points of the drivers you beat, byes excluded).
- Standard mode runs min(3, n-1) rounds. QMDRA mode runs exactly `RoundsToRun` rounds (can exceed n-1, causing deliberate rematches).

### Losers Bracket (post-RR)

- `LosersBracketBuilder.Build()` creates a single-elimination bracket for the buyback drivers.
- Pad to next power-of-two with BYE slots.
- Rematch avoidance: before each R1 pairing, check if the pair already appears in `PairingHistory`; if so, try to swap with a later driver.
- Subsequent rounds: winners of each pair advance; odd round sizes carry forward a BYE match.
- Output is `List<RandomMatch>` loaded into a `RandomEngineAdapter`.

### Final-4 (Finals phase)

- Always a Pro Ladder bracket over 3 or 4 drivers.
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
