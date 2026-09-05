# RC Drag Manager — Round Robin Mode: Full Design Specification  
**File:** 05_Mode_RoundRobin_Spec.md  
**Version:** 1.02  
**Status:** ✅ Stable (ChatGPT-Pack Ready)  
**Last Updated:** 2025-10-12  
**Owner:** Stewart McMillan  
**Source of Truth:** Derived from `MatchEngine_Refactor_Spec.md`, `PROJECT_STATUS.md`, and repository architecture.

---

## 🤖 How ChatGPT Should Use This Doc

This document defines the **Round Robin Race Mode** logic used in RC Drag Manager.  
Use it to understand:
- The 3-round limited qualifying structure.  
- How match pairings, lane assignments, and rankings are generated.  
- How fairness and randomness are enforced (no repeat pairings or lanes).  
- How this phase connects to `MatchEngine` and transitions to finals.

See also:  
- `03_Controller_Engine_Contracts.md` — controller & engine orchestration.  
- `04_Mode_Randomized_Bracket_Spec.md` — complementary elimination format.  
- `06_SQLite_Schema.md` — persistence schema.

---

## 🎯 Purpose

To run a **3-round Round Robin Qualifier** where each driver faces unique opponents and alternates between left and right lanes across rounds.  
This ensures fairness in both competition and lane distribution.

---

## 🧱 Goals

| Goal | Description |
|------|-------------|
| **Fixed 3 Rounds** | Exactly three rounds per session. |
| **No Repeat Pairings** | Drivers never meet the same opponent twice. |
| **Randomized Lanes** | Each driver randomly alternates between left and right lanes. |
| **Fairness Over Time** | Equal probability of lane usage across the event. |
| **Deterministic Rankings** | Stable points and tie-breaker results. |
| **Session Integrity** | All data serializable and replayable from save file. |

---

## 🔧 Architecture Overview

| File | Responsibility |
|------|----------------|
| `RoundRobinEngine.cs` | Core pairing, lane shuffle, and round progression logic. |
| `RoundRobinEngineAdapter.cs` | Implements `IRaceEngine` for controller integration. |
| `RoundRobinRanker.cs` | Computes standings and tie-breakers. |
| `MatchEngine.cs` | Manages RR → Losers → Pro Ladder transitions. |
| `RaceSession.cs` | Stores driver list, match data, and standings. |
| `Form1.cs` | Displays pairings, lanes, and results in the UI. |

---

## 🔄 Lifecycle Flow

### 1️⃣ Session Initialization
- User selects **Race Type → Round Robin (3-Round Qualifier)**.  
- `RaceController` creates a `RoundRobinEngineAdapter`.  
- Engine initializes the driver list and pairing history.

---

### 2️⃣ Pairing & Lane Generation

Each round’s match list is created using **controlled randomization** with two key rules:
1. **No repeat opponents** — enforced by `PairingHistory`.  
2. **Random lane assignment** — ensures lane variation for each driver.

---

### 🔹 Lane Shuffle Logic

Each match assigns drivers to left/right lanes randomly.  
`Form1` displays these via:
- **Label1 → Left Lane**
- **Label2 → Right Lane**

During generation:
```csharp
if (Random.Shared.Next(0, 2) == 1) {
    match.LeftLaneDriver = driverA;
    match.RightLaneDriver = driverB;
} else {
    match.LeftLaneDriver = driverB;
    match.RightLaneDriver = driverA;
}
```

This gives each driver a 50/50 chance of lane placement per round.  
Across 3 rounds, lanes will naturally balance (e.g., 2 left / 1 right or vice versa).

---

### 🔹 Optional Fairness Extension

For more consistent lane rotation, the engine can maintain:
```csharp
Dictionary<Guid, int> LeftLaneCount;
Dictionary<Guid, int> RightLaneCount;
```
Then bias assignment toward the lesser-used lane per driver.  
This is optional — the default version uses pure random fairness.

---

### 🔹 Example (6 Drivers)

| Round | Match | Left Lane | Right Lane |
|--------|--------|------------|-------------|
| 1 | M1 | Driver 2 | Driver 5 |
| 1 | M2 | Driver 3 | Driver 6 |
| 1 | M3 | Driver 1 | Driver 4 |
| 2 | M1 | Driver 6 | Driver 1 |
| 2 | M2 | Driver 5 | Driver 3 |
| 2 | M3 | Driver 2 | Driver 4 |
| 3 | M1 | Driver 4 | Driver 3 |
| 3 | M2 | Driver 1 | Driver 5 |
| 3 | M3 | Driver 6 | Driver 2 |

No repeats; lanes randomized each round.

---

### 3️⃣ Match Execution
- Each match appears on `Form1` with labeled lanes.  
- Race Director enters winners manually.  
- Engine logs each match:
  ```csharp
  Logger.LogMatchResult(matchId, leftLane, rightLane, winner);
  ```
- Results stored in `RoundData.Matches`.

---

### 4️⃣ Round Advancement
After each round:
- `RoundRobinEngine.IsComplete = true`  
- Controller triggers `Advance()` to start the next round.  
- After Round 3, engine signals end of phase:
  ```csharp
  if (RoundIndex == 3)
      Phase = RacePhase.Complete;
  ```

---

### 5️⃣ Ranking Calculation
Performed by `RoundRobinRanker` at Round 3 completion.

#### Ranking Priority
1. **Total Points**  
2. **Head-to-Head Result**  
3. **Opponent Score** — the win/loss points of the drivers you **beat**, added up (not everyone you raced)  
4. **Points Differential (Wins − Losses)**  
5. **Alphabetical Order** (fallback)

Each driver’s total lane distribution is optional diagnostic output in logs.

---

### 6️⃣ Transition to Finals
- **Top 3 drivers** → advance to **Pro Ladder Finals**.  
- **Remaining drivers** → sent to **Losers Bracket**.  
- Transition sequence in `MatchEngine`:
  ```
  RoundRobin → LosersBracket → ProLadder
  ```

---

## 🧮 Data Structures

### 🔹 Match Object
```csharp
class Match {
    Guid MatchId;
    Guid LeftLaneDriver;
    Guid RightLaneDriver;
    Guid? Winner;
    bool IsComplete;
}
```

### 🔹 Round Data
```csharp
class RoundData {
    int RoundNumber;
    List<Match> Matches;
}
```

### 🔹 Standings Table
```csharp
Dictionary<Guid, DriverStanding> Standings;
```

---

## 🗄️ Persistence & Logging

- Lane assignments and results serialized inside `RaceSession`.  
- All updates logged via `Logger.LogMatchResult()`.  
- Restoring session rehydrates lanes and standings exactly as saved — no re-randomization.

---

## 🔐 Rules & Constraints

| Rule | Description |
|------|-------------|
| **3 rounds fixed** | Exactly three rounds per event. |
| **No repeat pairings** | Controlled via `PairingHistory`. |
| **Random lane assignment** | 50/50 probability per match. |
| **Manual advancement** | Race Director controls progression. |
| **BYE fairness** | Each driver receives ≤1 BYE. |
| **Persistent lanes** | Lane assignments saved with session. |
| **No direct DB writes** | All data handled via repositories. |

---

## 📚 Adjacent Docs

| File | Purpose |
|------|----------|
| `03_Controller_Engine_Contracts.md` | Defines controller–engine interface |
| `04_Mode_Randomized_Bracket_Spec.md` | Companion elimination mode |
| `06_SQLite_Schema.md` | Database definitions |
| `07_Repository_Contracts.md` | Persistence boundaries |
| `09_Error_Handling_Logging.md` | Logging policy |
| `13_Project_Status_Summary.md` | Progress reference |

---

## ✅ Summary

The **Round Robin mode** in RC Drag Manager runs a **3-round, fair-pair qualifier** with random lane assignments each round.  
No drivers repeat opponents or lanes excessively, ensuring both competitive and environmental fairness.  
After 3 rounds, results automatically feed into the **Losers Bracket** and **Final-4 Ladder**, maintaining deterministic replay and complete session integrity.

---
