# RC Drag Manager — Code Structure v2.md

*Last Updated: 2025-06-21*

---

## 🔍 Overview

A WinForms application for running NHRA-style RC Drag Racing brackets. Supports three race modes:

* **Pro Ladder**: Seeded fixed-path elimination (NHRA)
* **Random Draw**: Unseeded, no rematch bracket
* **Round Robin**: 3 rounds, all-pairs with 1 BYE max/round

All match progression is round-by-round. No reseeding. No randomization in Pro Ladder. All race state flows from engines. UI is decoupled from logic.

---

## 🗂️ Directory Tree

```
RCDragManager\
├── Program.cs
├── Form1.cs
├── Form1.Designer.cs
├── LandingPageForm.cs
├── LoadSessionForm.cs
├── SessionSetupForm.cs
├── DriverManagerForm.cs
├── DriverStatsForm.cs
├── AddDriverDialog.cs
├── EditDriverDialog.cs
├── AddCarDialog.cs
├── SelectCarDialog.cs
├── AddEditQualTimeDialog.cs
├── EditWinnerDialog.cs
├── MatchEngine.cs
├── MatchResult.cs
├── ProLadder.cs
├── RandomMatchEngine.cs
├── RandomBracket.cs
├── RandomMatch.cs
├── RoundRobinEngine.cs
├── LosersBracketEngine.cs
├── RaceSession.cs
├── RaceSessionRepository.cs
├── Driver.cs
├── Car.cs
├── DriverRepository.cs
├── CarRepository.cs
├── MatchLookupHelper.cs
├── DatabaseInitializer.cs
├── App.config
```

---

## 📚 Namespaces

* `RCDragManagerProd` (main)
* `System.Windows.Forms`
* `System.Text.Json`, `System.IO` (persistence)
* `System.Linq`, `System.Collections.Generic`

---

## 📊 Class Index

### UI Forms

| Class                   | File                     | Base   | Purpose                               |
| ----------------------- | ------------------------ | ------ | ------------------------------------- |
| `Form1`                 | Form1.cs                 | `Form` | Main race control interface           |
| `LandingPageForm`       | LandingPageForm.cs       | `Form` | Entry form for starting/loading races |
| `SessionSetupForm`      | SessionSetupForm.cs      | `Form` | Creates new event, adds drivers/cars  |
| `LoadSessionForm`       | LoadSessionForm.cs       | `Form` | Loads saved session                   |
| `DriverManagerForm`     | DriverManagerForm.cs     | `Form` | Lists and manages driver records      |
| `DriverStatsForm`       | DriverStatsForm.cs       | `Form` | Shows driver win/loss stats           |
| `AddDriverDialog`       | AddDriverDialog.cs       | `Form` | Creates a new driver                  |
| `EditDriverDialog`      | EditDriverDialog.cs      | `Form` | Renames driver                        |
| `AddCarDialog`          | AddCarDialog.cs          | `Form` | Adds a car with class and dial-in     |
| `SelectCarDialog`       | SelectCarDialog.cs       | `Form` | Allows car selection from list        |
| `AddEditQualTimeDialog` | AddEditQualTimeDialog.cs | `Form` | Sets qualifying time                  |
| `EditWinnerDialog`      | EditWinnerDialog.cs      | `Form` | Manual match override UI              |

### Match Engines

| Class                 | File                   | Purpose                                |
| --------------------- | ---------------------- | -------------------------------------- |
| `MatchEngine`         | MatchEngine.cs         | Handles Pro Ladder bracket logic       |
| `RandomMatchEngine`   | RandomMatchEngine.cs   | Handles non-seeded bracket progression |
| `RoundRobinEngine`    | RoundRobinEngine.cs    | Rotational pairing for 3 rounds        |
| `LosersBracketEngine` | LosersBracketEngine.cs | Runs single-elim final from 4th+       |
| `MatchResult`         | MatchResult.cs         | Stores resolved match outcomes         |
| `MatchLookupHelper`   | MatchLookupHelper.cs   | Gets match info for display            |

### Data Models

| Class                   | File                     | Purpose                                        |
| ----------------------- | ------------------------ | ---------------------------------------------- |
| `Driver`                | Driver.cs                | Holds ID, name, stats, cars, seed, etc.        |
| `Car`                   | Car.cs                   | Class, default dial-in, name                   |
| `RaceSession`           | RaceSession.cs           | Serializable container for session save        |
| `RaceSessionRepository` | RaceSessionRepository.cs | Loads/saves sessions as JSON                   |
| `RandomMatch`           | RandomMatch.cs           | Match structure with resolved/unresolved state |

### Repositories

| Class                 | File                   | Notes                         |
| --------------------- | ---------------------- | ----------------------------- |
| `DriverRepository`    | DriverRepository.cs    | SQLite-backed driver storage  |
| `CarRepository`       | CarRepository.cs       | SQLite-backed car storage     |
| `DatabaseInitializer` | DatabaseInitializer.cs | Creates required DB structure |

### App Entry

| Class     | File       |
| --------- | ---------- |
| `Program` | Program.cs |

---

## 🚀 Flow of Control

### 1. App Launch

* `Program.cs` runs `Application.Run(new LandingPageForm())`
* User creates or loads a session
* Moves to `Form1` for race control

### 2. Engine Assignment

* `RaceType` in session determines engine:

  * Pro Ladder → `MatchEngine`
  * Random Draw → `RandomMatchEngine`
  * Round Robin → `RoundRobinEngine`

### 3. Round Progression

* Engines return current round’s matchups
* `Form1` tracks revealed rounds
* Results saved via `MatchResult`
* UI calls `SetWinner(...)` per match

### 4. Save / Load

* State saved via `RaceSessionRepository`
* Uses `RaceSession`, `SavedResults`, `SavedRevealedRounds`

### 5. Round Robin Add-on

* 3 rounds: R1, R2, R3
* Pairings generated using circle method
* Results stored in `MatchResult`
* 🔜 Ranking logic missing (planned in `RoundRobinRanker.cs`)

---

## ⚖️ Persistence Format

### `RaceSession`

```json
{
  "EventName": "Winter Nationals",
  "RaceType": "Round Robin",
  "DriverEntries": [ { ... } ],
  "SavedResults": [ { MatchId, WinnerId, LoserId } ],
  "SavedRevealedRounds": ["R1"]
}
```

---

## 🚜 Dependency Relationships

* `Form1.cs` → depends on: MatchEngine | RandomMatchEngine | RoundRobinEngine
* All Engines → depend on: `MatchResult`
* UI dialogs → depend on: `Driver`, `Car`, repositories
* `RaceSessionRepository` ←→ JSON storage layer
* `DriverRepository` / `CarRepository` ←→ SQLite DB layer

---

## 🔐 Notes

* All race logic is decoupled from UI
* Round Robin not yet fully wired
* Manual override supported for any match (via `EditWinnerDialog`)
* Tournament engines do not reseed or randomly reshuffle unless `RandomDraw`

---

## ✅ Next Steps (2025-06)

* [ ] Finalize RoundRobinRanker.cs
* [ ] UI: display RR standings after R3
* [ ] Save/load Round Robin match history properly
* [ ] Handle tied records (e.g. 2-1 vs 2-1) via tie-break rules
