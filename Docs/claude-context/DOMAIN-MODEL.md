# RC Drag Manager — Domain Model

## Glossary — the words the UI uses (#414)

Agreed with the Race Director after the August 2026 meet, where mixed wording
contributed to a class bracket being wiped (#413). **All user-facing text uses
these three words.** Code identifiers deliberately still use the older names —
renaming `RaceSession` and friends would churn the do-not-touch layer for no
operator benefit — so this table is the map between them.

| UI word | Means | Code type |
|---------|-------|-----------|
| **Event** | The whole thing: one day's tournament, containing one or more classes. | `MultiClassEvent` (a single-class event is just an event with one class) |
| **Class** | One field of drivers racing among themselves — one bracket, one tab in the console. | `RaceSession` |
| **Race** | Two drivers running against each other. | `EngineMatch` / "match" in code |
| **Round** | A stage within a class: RR1, RR2, SF, F, … | `RoundLabels` constants |

Rules that follow from this:

- Never say "session" or "match" in user-facing text — say "class" or "race".
- Destructive confirmations must name their scope: "Close **this class**?",
  not "Close this race?".
- "Race" in the singular means two drivers. If it covers a whole bracket, the
  word wanted is "class".
- Bracket cards still label races `M1`, `M2`, … The `M` is kept on purpose:
  `R1` would read as a round next to the `RR1`/`RR2` round labels.

---

## Core Entities

### Driver (`Domain/Drivers.cs`)

Represents a registered racing participant.

| Property | Type | Notes |
|----------|------|-------|
| `Id` | `int` | Runtime-assigned incrementing integer (thread-safe via `Interlocked`). Attempts to set to 0 are ignored. When loaded from DB, the DB integer PK is used directly. |
| `Name` | `string` | Display name shown in bracket |
| `QualTime` | `double?` | Qualifying lap time in seconds. `null` = no time recorded. Lower = faster = higher seed. |
| `Notes` | `string` | Freeform notes |
| `TotalWins` | `int` | Lifetime race wins (across all events) |
| `TotalLosses` | `int` | Lifetime race losses |
| `EventsEntered` | `int` | Number of events participated in |
| `EventsWon` | `int` | Number of events won (final champion) |
| `Seed` | `int?` | Seed position assigned during bracket generation. 1 = fastest. Not persisted; recalculated on bracket build. |
| `State` | `string` | Optional location/state field for the driver |
| `Cars` | `List<Car>` | Vehicles owned by this driver |

---

### Car (`Domain/Car.cs`)

A vehicle registered to a driver. Drivers can have multiple cars.

| Property | Type | Notes |
|----------|------|-------|
| `Id` / `CarID` | `int` | Database PK. `CarID` is an alias property that maps to `Id` (legacy compatibility). |
| `DriverId` | `int` | FK to owning Driver |
| `CarName` | `string` | Free-text name (e.g., "Rlaarlo AM-X10") |
| `ClassType` | `string` | Race class: `"Heads Up"`, `"Bracket"`, `"Dial-In"`, etc. |
| `DefaultDialIn` | `double?` | Default dial-in time for bracket racing. `null` for heads-up. |

---

### RaceSession (`Domain/RaceSession.cs`)

The central session object. Created at session setup, passed to `Form1` and `RaceController`, and serialized to JSON for persistence.

| Property | Type | Notes |
|----------|------|-------|
| `Id` | `int` | DB-assigned integer PK after save. 0 before first save. |
| `EventName` | `string` | Human-readable event name (e.g., "Club Race March 2026") |
| `EventDate` | `DateTime` | Date of the event |
| `RaceType` | `string` | `"Pro Ladder"`, `"Round Robin"`, `"Random"`, `"Losers Bracket"`, `"Finals"` — mutates during the event as phases change |
| `ClassType` | `string` | Car class for this session (e.g., `"Heads Up"`) |
| `FixedDialIn` | `double?` | If set, all drivers use this dial-in (index racing) |
| `RoundRobinVariant` | `string` | `"Standard"` (default) or `"QMDRA"` |
| `RoundsToRun` | `int?` | For QMDRA mode: exact number of RR rounds to run. `null` in Standard mode. |
| `DriverEntries` | `List<RaceSessionDriverEntry>` | Snapshot of participating drivers + car + dial-in at session creation |
| `PairingHistoryRaw` | `List<int[]>` | **Serialization backing store** for pairing history. Each `int[]` is `[id1, id2]`. |
| `PairingHistory` | `HashSet<(int,int)>` | **`[JsonIgnore]`** Computed from `PairingHistoryRaw`. Normalized pairs (smaller Id first) of every driver matchup in the event. Used for rematch avoidance. |
| `SavedResults` | `List<MatchResultSave>` | Match winners/losers at save time |
| `SavedRevealedRounds` | `List<string>` | Which round labels had been revealed at save time |
| `Matches` | `List<RandomMatch>` | Random/LB bracket matches (populated during Random mode) |
| `RoundRobinMatches` | `List<RoundRobinMatch>` | RR match records |
| `Drivers` | `List<Driver>` | Live driver list for the session (kept in sync with controller) |
| `BuybackDrivers` | `List<Driver>` | Drivers selected for the Losers Bracket after RR phase |
| `TopDriversSnapshot` | `List<Driver>` | Legacy/snapshot field |

**Important:** `RaceType` is mutable. It starts as the chosen race type but is overwritten as phases transition: `"Round Robin"` → `"Losers Bracket"` → `"Finals"`.

---

### RaceSessionDriverEntry (`Domain/RaceSession.cs`)

A point-in-time snapshot of one driver's participation in a session. Stored inside `RaceSession.DriverEntries`.

| Property | Type | Notes |
|----------|------|-------|
| `DriverID` | `int` | FK to Driver |
| `DriverName` | `string` | Name at time of session creation |
| `CarID` | `int` | FK to Car |
| `CarName` | `string` | Car name at time of session creation |
| `ClassType` | `string` | Class at time of session creation |
| `DialIn` | `double?` | Per-entry dial-in override |
| `QualifyingTime` | `double?` | Qualifying time at session creation |
| `Seed` | `int?` | Manual seed override (Pro Ladder only) |

---

### MatchResult (`Domain/MatchResult.cs`)

In-memory store of race outcomes. Keyed by integer `matchId`.

| Method | Purpose |
|--------|---------|
| `SetWinner(matchId, winner, loser)` | Record outcome |
| `GetWinner(matchId)` | Retrieve winner |
| `GetLoser(matchId)` | Retrieve loser |
| `HasResult(matchId)` / `IsMatchResolved(matchId)` | Check if resolved |
| `ClearFromMatch(matchId)` | Remove all results from `matchId` upward (for result editing) |
| `IsTournamentComplete(bracketMatches)` | True if the Final match has a winner |
| `GetAllPairings()` | Returns all pairings as normalized `(int,int)` tuples |
| `GetAllResults()` | Returns `(WinnerId, LoserId)` tuples |
| `Clear()` | Wipe all results |

`MatchResult` is **not serialized**. Results are reconstructed from `RaceSession.SavedResults` on session load.

---

### MatchResultSave (`Domain/RaceSession.cs`)

Lightweight serializable record of one match outcome, stored in `RaceSession.SavedResults`.

| Property | Type |
|----------|------|
| `MatchId` | `int` |
| `WinnerDriverId` | `int` |
| `LoserDriverId` | `int` |

---

### ProLadder.LadderMatch (`Domain/ProLadder.Structures.cs`)

A single match definition in the NHRA Pro Ladder bracket template.

| Property | Type | Notes |
|----------|------|-------|
| `MatchId` | `int` | Sequential integer, 1-based |
| `Seed1` | `int?` | Seed of Driver 1 if known at bracket creation (Round 1 only) |
| `Seed2` | `int?` | Seed of Driver 2 if known |
| `FromMatch1` | `int?` | MatchId whose winner becomes Driver 1 (later rounds) |
| `FromMatch2` | `int?` | MatchId whose winner becomes Driver 2 |
| `RoundLabel` | `string` | e.g., `"R1"`, `"SF"`, `"F"` |

---

### RandomMatch (`RandomMode/RandomMatch.cs`)

Match node for Random Draw and Losers Bracket brackets. The same data structure serves both use cases.

| Property | Type | Notes |
|----------|------|-------|
| `MatchId` | `int` | |
| `Seed1` | `Driver` | Direct driver reference (null = BYE or unknown) |
| `Seed2` | `Driver` | |
| `FromMatch1` | `int?` | MatchId whose winner populates Seed1 in later rounds |
| `FromMatch2` | `int?` | MatchId whose winner populates Seed2 |
| `RoundLabel` | `string` | Normalized label: `"R1"`, `"LB-R1"`, `"LB-F"`, etc. |

---

### RoundRobinMatch (`RoundRobinMode/RoundRobinMatch.cs`)

Match record for a Round Robin round.

| Property | Type |
|----------|------|
| `MatchId` | `int` |
| `RoundLabel` | `string` (e.g., `"RR1"`, `"RR2"`) |
| `Driver1` | `Driver` |
| `Driver2` | `Driver` (null = BYE) |

---

### EngineMatch (`RaceEngines/IRaceEngine.cs`)

Neutral DTO exposed by all `IRaceEngine` implementations. The controller and UI only see this type — never engine-internal types.

| Property | Type |
|----------|------|
| `MatchId` | `int` |
| `Driver1` | `Driver` |
| `Driver2` | `Driver` |
| `RoundLabel` | `string` |
| `FromMatch1` | `int?` |
| `FromMatch2` | `int?` |
| `HasResult` | `bool` |

---

### DriverRankResult (`RoundRobinMode/RoundRobinRanker.cs`)

Output of the RR ranking process. One entry per driver.

| Property | Type | Notes |
|----------|------|-------|
| `DriverId` | `int` | |
| `Rank` | `int` | 1 = top |
| `Points` | `double` | Win=4, Loss=1, BYE=2 |
| `Wins` | `int` | |
| `Losses` | `int` | |
| `DefeatedIds` | `int[]` | IDs of drivers beaten |
| `OpponentStrength` | `double` | Sum of final points of opponents faced (strength-of-schedule) |

---

### ViewModels

Used to pass display data from controller to UI — no engine types leak into forms.

| Class | Purpose |
|-------|---------|
| `PairingRow` | One row in the bracket list: `MatchId`, `RoundLabel`, `Driver1`, `Driver2`, `IsHeader` (bold round header row) |
| `WinnerRow` | One entry in the winners list: `MatchId`, `RoundLabel`, `Winner`, `Loser` |
| `RaceSessionSummary` | Summary row for LoadSession list: `Id`, `EventName`, `EventDate`, `ClassType`, `RaceType` |
| `MatchResultSave` | Serialization form of a result — also lives in `Domain/RaceSession.cs` |

---

## Key Enums and Constants

### Round Labels

Round labels are **strings** (not enums). `RoundLabels.cs` normalizes them to canonical forms:

| Canonical Label | Meaning |
|----------------|---------|
| `"R1"`, `"R2"`, … | Winners bracket rounds |
| `"SF"` | Semi-final |
| `"F"` | Final |
| `"RR1"`, `"RR2"`, … | Round Robin rounds |
| `"LB-R1"`, `"LB-R2"`, … | Losers Bracket rounds |
| `"LB-F"` | Losers Bracket Final |

Sort order (from `RoundLabels.CompareKey`): RR rounds (100+) → Winners rounds (200+) → SF (290) → F (299) → LB rounds (400+) → LB-F (499).

### Race Types (RaceSession.RaceType)

| Value | When set |
|-------|---------|
| `"Pro Ladder"` | NHRA Pro Ladder mode |
| `"Round Robin"` | RR mode initial phase |
| `"Random"` | Random draw mode |
| `"Losers Bracket"` | After RR, when LB phase starts |
| `"Finals"` | Final bracket phase (Pro Ladder engine) |

### Round Robin Variants

| Value | Meaning |
|-------|---------|
| `"Standard"` | Runs up to min(3, n−1) rounds; top-3 advance + LB winner |
| `"QMDRA"` | Runs exactly `RoundsToRun` rounds; all drivers advance to finals in ranked order |

### BYE Policy

A driver is a BYE if and only if `driver == null`. `ByePolicy.IsBye(d)` encapsulates this.

---

## Entity Relationships

```
Driver (1) ────── (0..*) Car
Driver (*) ────── (*) RaceSession  [via RaceSessionDriverEntry]
RaceSession (1) ── (0..*) RandomMatch
RaceSession (1) ── (0..*) RoundRobinMatch
RaceSession (1) ── (0..*) MatchResultSave
MatchResult (1) ── (0..*) (matchId → Winner/Loser)  [in-memory, not persisted directly]
ProLadder.GetLadder(n) → List<LadderMatch>  [static templates, not stored in DB]
```
