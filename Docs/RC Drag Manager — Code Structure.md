# RC Drag Manager — Code Structure Reference  
*Generated 19 Jun 2025 — keep this file up-to-date.*

---

## 📁 Directory Tree (Repo Root)

├─ AddCarDialog.cs
├─ AddCarDialog.Designer.cs
├─ AddDriverAndCarDialog.cs
├─ AddDriverAndCarDialog.Designer.cs
├─ AddDriverDialog.cs
├─ AddDriverDialog.Designer.cs
├─ AddEditQualTimeDialog.cs
├─ AddEditQualTimeDialog.Designer.cs
├─ App.config
├─ Car.cs
├─ CarRepository.cs
├─ DatabaseInitializer.cs
├─ DriverManagerForm.cs
├─ DriverManagerForm.Designer.cs
├─ DriverManagerForm.resx
├─ DriverRepository.cs
├─ DriverStatsForm.cs
├─ DriverStatsForm.Designer.cs
├─ Drivers.cs
├─ EditDriverDialog.cs
├─ EditDriverDialog.Designer.cs
├─ EditWinnerDialog.cs
├─ EditWinnerDialog.Designer.cs
├─ Form1.cs
├─ Form1.Designer.cs
├─ Form1.resx
├─ LandingPageForm.cs
├─ LandingPageForm.Designer.cs
├─ LandingPageForm.resx
├─ LoadSessionForm.cs
├─ LoadSessionForm.Designer.cs
├─ LoadSessionForm.resx
├─ LosersBracketEngine.cs
├─ MatchEngines/
│ ├─ IMatchEngine.cs
│ ├─ MatchEngine.cs
│ ├─ ProLadderEngine.cs
│ └─ RacePhase.cs
├─ MatchLookupHelper.cs
├─ MatchResult.cs
├─ packages.config
├─ Program.cs
├─ ProLadder.cs
├─ RaceSession.cs
├─ RaceSessionRepository.cs
├─ RandomBracket.cs
├─ RandomMatch.cs
├─ RandomMatchEngine.cs
├─ rcdrag_logo 2.ico
├─ RCDragManagerProd.csproj
├─ RCDragManagerProd.sln
├─ Reto logo trans full 256.png
├─ Reto logo trans full.png
├─ retro trans icon.ico
├─ RoundRobinEngine.cs
├─ RoundRobinRanker.cs
└─ (Designer .resx & icon files omitted above for brevity)

markdown
Copy
Edit

---

## 🗂️ Namespaces & Core Types

### `RCDragManagerProd.MatchEngines`
| Type | File | Implements / Base |
|------|------|-------------------|
| `interface IMatchEngine` | `IMatchEngine.cs` | — |
| `class MatchEngine` | `MatchEngine.cs` | `IMatchEngine` |
| `class ProLadderEngine` | `ProLadderEngine.cs` | `IMatchEngine` |
| `class EngineMatch` | `ProLadderEngine.cs` | (helper record) |
| `enum RacePhase` | `RacePhase.cs` | — |

### `RCDragManagerProd` (root)
*Highlights only — see full table below for every class.*

- **Forms / UI**  
  `Form1`, `LandingPageForm`, `SessionSetupForm`, `LoadSessionForm`, `DriverManagerForm`, `DriverStatsForm`, plus all add/edit dialogs. All inherit `System.Windows.Forms.Form`.

- **Domain Models**  
  `Driver`, `Car`, `RaceSession`, `MatchResult`, `RandomMatch`, `RandomBracket`.

- **Engines**  
  `RandomMatchEngine`, `LosersBracketEngine`, `RoundRobinEngine`, `RoundRobinRanker`.

- **Repositories / Data**  
  `DriverRepository`, `CarRepository`, `RaceSessionRepository`, `DatabaseInitializer`.

- **Helpers**  
  `MatchLookupHelper`, `ProLadder` (hard-coded NHRA ladders).

---

## 📋 Class Index (complete)

| Class / Interface / Enum | Kind | Source File | Namespace | Inherits / Implements |
|--------------------------|------|-------------|-----------|-----------------------|
| AddCarDialog | class | AddCarDialog.cs | RCDragManagerProd | Form |
| AddDriverAndCarDialog | class | AddDriverAndCarDialog.cs | RCDragManagerProd | Form |
| AddDriverDialog | class | AddDriverDialog.cs | RCDragManagerProd | Form |
| AddEditQualTimeDialog | class | AddEditQualTimeDialog.cs | RCDragManagerProd | Form |
| Car | class | Car.cs | RCDragManagerProd | — |
| CarRepository | class | CarRepository.cs | RCDragManagerProd | — |
| DatabaseInitializer | class | DatabaseInitializer.cs | RCDragManagerProd | — |
| Driver | class | Drivers.cs | RCDragManagerProd | — |
| DriverManagerForm | class | DriverManagerForm.cs | RCDragManagerProd | Form |
| DriverRepository | class | DriverRepository.cs | RCDragManagerProd | — |
| DriverStatsForm | class | DriverStatsForm.cs | RCDragManagerProd | Form |
| EditDriverDialog | class | EditDriverDialog.cs | RCDragManagerProd | Form |
| EditWinnerDialog | class | EditWinnerDialog.cs | RCDragManagerProd | Form |
| Form1 | class | Form1.cs | RCDragManagerProd | Form |
| IMatchEngine | interface | MatchEngines/IMatchEngine.cs | RCDragManagerProd.MatchEngines | — |
| LandingForm | class | LandingPageForm.cs | RCDragManagerProd | Form |
| LoadSessionForm | class | LoadSessionForm.cs | RCDragManagerProd | Form |
| LosersBracketEngine | class | LosersBracketEngine.cs | RCDragManagerProd | — |
| MatchEngine | class | MatchEngines/MatchEngine.cs | RCDragManagerProd.MatchEngines | IMatchEngine |
| MatchLookupHelper | class | MatchLookupHelper.cs | RCDragManagerProd | — |
| MatchResult | class | MatchResult.cs | RCDragManagerProd | — |
| ProLadder | static class | ProLadder.cs | RCDragManagerProd | — |
| ProLadderEngine | class | MatchEngines/ProLadderEngine.cs | RCDragManagerProd.MatchEngines | IMatchEngine |
| RacePhase | enum | MatchEngines/RacePhase.cs | RCDragManagerProd.MatchEngines | — |
| RaceSession | class | RaceSession.cs | RCDragManagerProd | — |
| RaceSessionDriverEntry | class | RaceSession.cs | RCDragManagerProd | — |
| MatchResultSave | class | RaceSession.cs | RCDragManagerProd | — |
| RaceSessionRepository | class | RaceSessionRepository.cs | RCDragManagerProd | — |
| RaceSessionSummary | class | RaceSessionRepository.cs | RCDragManagerProd | — |
| RandomBracket | class | RandomBracket.cs | RCDragManagerProd | — |
| RandomMatch | class | RandomMatch.cs | RCDragManagerProd | — |
| RandomMatchEngine | class | RandomMatchEngine.cs | RCDragManagerProd | — |
| RoundRobinEngine | class | RoundRobinEngine.cs | RCDragManagerProd | — |
| RoundRobinRanker | class | RoundRobinRanker.cs | RCDragManagerProd | — |
| SelectCarDialog | class | SelectCarDialog.cs | RCDragManagerProd | Form |
| SessionSetupForm | class | SessionSetupForm.cs | RCDragManagerProd | Form |
| Program | static class | Program.cs | RCDragManagerProd | — |

*(Designer/resx files contain auto-generated partial classes and are omitted for clarity.)*

---

## 🔗 Key Dependencies & Flow

1. **Repositories** (`DriverRepository`, `CarRepository`, `RaceSessionRepository`) provide all data persistence; forms and engines receive repository instances via constructor injection.
2. **Engines**
   - `MatchEngine` (implements `IMatchEngine`) is the generic bracket handler.
   - `ProLadderEngine` implements NHRA-style seeded ladders (uses static `ProLadder` map).
   - `RandomMatchEngine`, `LosersBracketEngine`, and `RoundRobinEngine` each implement custom pairing logic while conforming to `IMatchEngine`.
3. **UI Layer**
   - `Form1` hosts race-control UI and instantiates the required *engine* based on race type selected in `SessionSetupForm`.
   - Child dialogs (`Add*`, `Edit*`, `SelectCarDialog`, etc.) update repositories, which fire events consumed by the manager forms.
4. **Domain Flow**
   - `RaceSession` aggregates `RaceSessionDriverEntry` objects and current `RacePhase`.
   - `MatchResult` captures finished match data; helpers like `MatchLookupHelper` locate next pairings.
5. **Startup**
   - `Program.cs` → `LandingForm` → create/open session via `SessionSetupForm` → launch `Form1` with chosen `IMatchEngine` → repositories and engines coordinate via events/callbacks.

---

### ✅ Maintenance Notes
- **Add new files/classes:** append to the *Class Index* table and update the directory tree if location changes.
- **Refactor:** update inheritance/implements column to keep the dependency map accurate.
- **Designer partials:** no manual edits required; Visual Studio regenerates them automatically.

*End of file*