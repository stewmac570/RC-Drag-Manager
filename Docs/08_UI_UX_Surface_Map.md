# RC Drag Manager — UI / UX Surface Map  
**File:** 08_UI_UX_Surface_Map.md  
**Version:** 1.00  
**Status:** ✅ Stable (ChatGPT-Pack Ready)  
**Last Updated:** 2025-10-12  
**Owner:** Stewart McMillan  
**Source of Truth:** Derived from repository code (`Form1.cs`, `RoundRobinEngineAdapter.cs`, `MatchEngine.cs`) and verified behavior.

---

## 🤖 How ChatGPT Should Use This Doc

Use this file to understand **how the user interface is structured and bound** to the underlying race engines.  
It defines all visible panels, labels, and buttons used across:
- Round Robin qualifiers  
- Randomized Bracket eliminations  
- Pro Ladder finals  

Focus points:
- Lane display logic (`Label1` = Left, `Label2` = Right).  
- How session progression is shown and updated.  
- How user actions map to engine/controller methods.  

See also:  
- `03_Controller_Engine_Contracts.md` — controller ↔ UI integration.  
- `05_Mode_RoundRobin_Spec.md` — lane shuffle logic.  
- `06_SQLite_Schema.md` — data persistence.  
- `09_Error_Handling_Logging.md` — user-visible error messaging.

---

## 🧱 Overall Layout (Form 1)

| Element | Type | Purpose |
|----------|------|----------|
| **Form1** | Main WinForms window | Core application surface. Hosts all mode views and race control buttons. |
| **Panel_Header** | Panel | Displays app title, current mode, and event name. |
| **Panel_RoundView** | Panel | Shows current Round Robin / Random Bracket pairings and results. |
| **Panel_BracketView** | Panel | Displays Pro Ladder visualization (top 4). |
| **Panel_Summary** | Panel | Displays standings, rankings, and stats. |
| **Panel_Settings** | Panel | Hidden settings/config pane. |

---

## 🧭 UI State Flow

```
Startup  →  ModeSelectionDialog
             ↓
           Form1 (Session Active)
             ↓
      RoundView ↔ BracketView ↔ SummaryView
             ↓
         Session Complete
```

Each mode panel is dynamically populated based on `RacePhase`.

---

## 🧩 Header Panel Elements

| Control | Type | Description |
|----------|------|-------------|
| `lblEventName` | Label | Displays the current session/event name. |
| `lblPhase` | Label | Shows current race phase (“Round 1 of 3”, “Pro Ladder”, etc.). |
| `btnNewSession` | Button | Launches mode selector (Round Robin / Random / Pro Ladder). |
| `btnSaveSession` | Button | Triggers `RaceSessionRepository.SaveSession()`. |
| `btnLoadSession` | Button | Opens saved session picker. |

---

## 🏁 Round View (Round Robin / Random Bracket)

### 🔹 Dynamic Pairing Display

| Control | Type | Function |
|----------|------|----------|
| `lblLeftLane` *(Label1)* | Label | Displays driver assigned to left lane (`Match.LeftLaneDriver`). |
| `lblRightLane` *(Label2)* | Label | Displays driver assigned to right lane (`Match.RightLaneDriver`). |
| `lblRoundNumber` | Label | “Round 1 / 3” etc. |
| `btnSetWinnerLeft` | Button | Sets left lane driver as winner. |
| `btnSetWinnerRight` | Button | Sets right lane driver as winner. |
| `lblWinnerDisplay` | Label | Shows “Winner: <Name>”. |
| `btnNextRound` | Button | Calls `RaceController.AdvanceRound()`. |
| `btnBack` | Button | Returns to previous screen or mode. |

### 🔹 Lane Shuffle Behavior

- When each match is loaded, Form1 reads:
  ```csharp
  lblLeftLane.Text = driverRepo.GetName(match.LeftLaneDriver);
  lblRightLane.Text = driverRepo.GetName(match.RightLaneDriver);
  ```
- Lanes are assigned by the engine during pairing (see `05_Mode_RoundRobin_Spec.md`).
- Label colors indicate lanes:
  - **Left Lane = Blue** (`#3478C3`)
  - **Right Lane = Red** (`#C03434`)
- Optional tooltip shows historical lane count for fairness tracking.

---

## 🧩 Bracket View (Pro Ladder)

| Control | Type | Function |
|----------|------|----------|
| `Panel_Bar1–4` | Panels | Represent the Final-4 ladder bars. |
| `lblSemiA1 / lblSemiA2` | Labels | Semifinal A drivers. |
| `lblSemiB1 / lblSemiB2` | Labels | Semifinal B drivers. |
| `lblFinal1 / lblFinal2` | Labels | Final round drivers. |
| `btnRunSemiA / btnRunSemiB / btnRunFinal` | Buttons | Commit winners. |

**Data Flow:**  
Ladder data retrieved from `MatchEngine.GetCurrentBracket()` and bound live.

---

## 🧩 Summary View (Standings)

| Control | Type | Function |
|----------|------|----------|
| `dgvStandings` | DataGridView | Displays ranked standings with points, wins/losses, lane stats. |
| `btnExportCSV` | Button | Exports current standings to CSV. |
| `lblTotalRounds` | Label | “3 Rounds Completed”. |
| `lblNextPhase` | Label | “Advancing to Final 4”. |

**Data Source:**  
`RoundRobinRanker.GetStandings()` or `Repository.LoadSession().StandingsData`.

---

## ⚙️ Settings Panel (Hidden / Advanced)

| Control | Type | Description |
|----------|------|-------------|
| `chkEnableLogging` | Checkbox | Toggles persistent logging. |
| `cmbLaneBias` | ComboBox | Options: “Random”, “Balanced”. |
| `btnClearSessions` | Button | Deletes local session cache. |
| `btnExportConfig` | Button | Saves current settings to JSON. |

---

## 🎨 Visual & UX Conventions

| Aspect | Description |
|--------|-------------|
| **Color coding** | Blue = Left lane, Red = Right lane, Gold = Winner. |
| **Font hierarchy** | Header (16 pt bold), Pairings (14 pt regular), Metadata (10 pt grey). |
| **Animations** | Optional fade-in for lane labels when rounds load. |
| **Error alerts** | Non-blocking message box with `[ErrorCode]` from logger. |
| **Progress bar** | Optional in-header round-progress indicator. |

---

## 🔄 UI ↔ Controller Binding Map

| UI Action | Controller Call | Engine Target |
|------------|----------------|---------------|
| Click `SetWinnerLeft` | `RaceController.SetWinner(matchId, leftDriverId)` | `RoundRobinEngineAdapter.SetWinner()` |
| Click `SetWinnerRight` | Same as above with right ID | — |
| Click `NextRound` | `RaceController.AdvanceRound()` | `RoundRobinEngine.Advance()` |
| Click `SaveSession` | `RaceSessionRepository.SaveSession()` | — |
| Click `LoadSession` | `RaceSessionRepository.LoadSession()` | — |
| Click `NewSession` | `ModeSelectionDialog.Open()` | — |

---

## 🧩 Data Binding Example

```csharp
Private Sub LoadMatch(match As Match)
    lblLeftLane.Text = driverRepo.GetName(match.LeftLaneDriver)
    lblRightLane.Text = driverRepo.GetName(match.RightLaneDriver)
    lblWinnerDisplay.Text = If(match.WinnerId Is Nothing, "", "Winner: " & driverRepo.GetName(match.WinnerId))
End Sub
```

Lane colors and labels are updated together from the `Match` record.

---

## 🧱 Adjacent Docs

| File | Purpose |
|------|----------|
| `05_Mode_RoundRobin_Spec.md` | Defines pairing & lane shuffle rules. |
| `06_SQLite_Schema.md` | Schema reference for UI-bound data. |
| `07_Repository_Contracts.md` | Persistence and transaction behavior. |
| `09_Error_Handling_Logging.md` | UI error and logging policy. |
| `13_Project_Status_Summary.md` | Development status reference. |

---

## ✅ Summary

The **RC Drag Manager UI** is structured around a clear surface model where each visual control directly reflects live engine data.  
Lane shuffle outputs (`LeftLaneDriver`, `RightLaneDriver`) are shown through color-coded labels, and all user actions are routed through the `RaceController` for deterministic updates.  
This ensures stable operation across sessions, reproducible race flows, and user transparency during multi-round events.

---
