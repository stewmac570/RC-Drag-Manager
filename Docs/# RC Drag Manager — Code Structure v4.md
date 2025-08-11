\# RC Drag Manager — Code Structure v4



\*Version: 4.0.0\*

\*Author: Stewart McMillan\*

\*Generated: 2025-08-08\*



---



\## ✨ Overview



\*\*RC Drag Manager\*\* is a modular C# WinForms application for managing NHRA-style RC drag race brackets. It supports multiple race modes, uses in-memory race engines with UI-disconnected logic, and supports persistent driver management, car assignment, and bracket saving.



This document maps the full architecture based on the actual codebase. It includes:



\* File-by-file breakdown

\* Folder map

\* Layered responsibilities

\* Engine flow

\* Race session design

\* Bracket engines

\* UI dialogs

\* Runtime flow

\* Event-to-method routing



---



\## 📁 Folder Map



```

RCDragManagerProd/

├── App.config                      # Logging, settings

├── Program.cs                     # App entry point

├── Controllers/

│   └── RaceController.cs          # Central bracket and match flow

├── RaceEngines/

│   ├── IRaceEngine.cs            # Engine contract

│   ├── ProLadderEngineAdapter.cs # NHRA bracket logic

│   ├── RandomEngineAdapter.cs    # Random draw wrapper

│   ├── RoundRobinEngineAdapter.cs# RR adapter

│   └── RaceEngineFactory.cs      # Creates engine per race type

├── RandomMode/

│   ├── RandomBracket.cs          # Random match generator

│   ├── RandomMatch.cs            # Match model

│   └── RandomMatchEngine.cs      # Match result tracker

├── RoundRobinMode/

│   ├── RoundRobinEngine.cs       # Core RR engine

│   ├── RoundRobinMatch.cs        # Match model

│   └── RoundRobinRanker.cs       # RR ranking logic

├── Helpers/

│   ├── MatchLookupHelper.cs      # NHRA match trace utility

├── ViewModels/

│   ├── PairingRow.cs             # Bracket ListView row

│   ├── WinnerRow.cs              # Results ListView row

│   └── MatchResultSave.cs        # Match result snapshot

├── Domain/

│   ├── Driver.cs                 # Racer

│   ├── Car.cs                    # Car linked to driver

│   ├── RaceSession.cs           # In-memory session

│   ├── MatchResult.cs           # Live match result tracker

│   └── ProLadder.cs             # Static NHRA ladder data

├── Repositories/

│   ├── DriverRepository.cs       # DB ops

│   ├── CarRepository.cs          # DB ops

│   └── RaceSessionRepository.cs  # Save/load sessions

├── UI/Forms/

│   ├── Form1.cs, .Designer.cs

│   ├── LandingPageForm.cs        # Entry screen

│   ├── SessionSetupForm.cs       # Full setup form

│   ├── LoadSessionForm.cs        # Load from DB

│   ├── DriverManagerForm.cs      # Manage driver list

│   ├── DriverStatsForm.cs        # W/L stats

│   ├── EditWinnerDialog.cs       # Manual winner override

│   ├── BuybackDriverSelectionForm.cs # RR loser entry

│   └── All Add/Edit Dialogs      # Driver, car, qual time

```



---



\## 🧪 Layer Responsibilities



\### Controllers



\* \*\*RaceController.cs\*\*



&nbsp; \* Connects UI to engine logic

&nbsp; \* Drives bracket flow: generate, advance, inject

&nbsp; \* Raises events: `BracketRedrawn`, `NextMatchReady`



\### Race Engine Adapters



\* \*\*IRaceEngine.cs\*\*: interface with `LoadDrivers()`, `GetMatches()`, `SetWinner()`, `AdvanceRound()`

\* \*\*ProLadderEngineAdapter.cs\*\*: wraps MatchEngine using `ProLadder.cs`

\* \*\*RandomEngineAdapter.cs\*\*: wraps `RandomMatchEngine`, allows injected matches

\* \*\*RoundRobinEngineAdapter.cs\*\*: wraps `RoundRobinEngine`, tracks standings

\* \*\*RaceEngineFactory.cs\*\*: creates adapters by race type



\### Core Engine Backends



\* \*\*MatchEngine.cs\*\*: NHRA-style bracket handler

\* \*\*RandomMatchEngine.cs\*\*: match + result tracker

\* \*\*RandomBracket.cs\*\*: generates fair random bracket

\* \*\*RoundRobinEngine.cs\*\*: builds all-play match matrix

\* \*\*RoundRobinRanker.cs\*\*: scores RR rankings using W/L, H2H, SoS



\### Domain Models



\* \*\*Driver.cs\*\*: name, seed, ID

\* \*\*Car.cs\*\*: linked to Driver

\* \*\*RaceSession.cs\*\*: live tournament state (engine, revealed rounds, match list, results)

\* \*\*MatchResult.cs\*\*: match → winner map

\* \*\*ProLadder.cs\*\*: hardcoded NHRA ladder map



\### Repositories



\* \*\*DriverRepository.cs\*\*: saves drivers

\* \*\*CarRepository.cs\*\*: saves cars

\* \*\*RaceSessionRepository.cs\*\*: saves full session



\### ViewModels



\* \*\*PairingRow\\.cs\*\*: shows each match in ListView

\* \*\*WinnerRow\\.cs\*\*: shows result list

\* \*\*MatchResultSave.cs\*\*: output-friendly result map



\### UI Layer



\* \*\*Form1.cs\*\*: central UI



&nbsp; \* Button: Generate Bracket → `RaceController.GenerateBracket()`

&nbsp; \* Button: Advance Round → `RaceController.AdvanceRound()`

&nbsp; \* Match buttons: select winners → `RaceController.SubmitWinner()`

\* \*\*SessionSetupForm.cs\*\*: enter drivers, seeds, select class

\* \*\*LandingPageForm.cs\*\*: quick start or load/save session

\* \*\*DriverManagerForm.cs\*\*: add/remove drivers from permanent list

\* \*\*Add/Edit/SelectCar/Driver/QualTime dialogs\*\*: UI helpers

\* \*\*BuybackDriverSelectionForm.cs\*\*: after RR complete, lets losers opt-in



---



\## 🏋️ Race Flow Summary



```

Startup → LandingPageForm

&nbsp;   └── new session → SessionSetupForm

&nbsp;           └── Enter drivers/cars/qual times

&nbsp;           └── Select Race Mode (Pro, Random, RR)



Generate Bracket → RaceController

&nbsp;   └── RaceEngineFactory returns correct IRaceEngine

&nbsp;   └── Drivers sorted/seated and passed to adapter



Race Begins → Match list rendered → UI waits for winner input



Submit Winner → RaceController.SubmitWinner()

&nbsp;   └── Engine.SetWinner()

&nbsp;   └── MatchResult.SetWinner(matchId, D1, D2)



Advance Round → RaceController.AdvanceRound()

&nbsp;   └── If RR complete → triggers Buyback dialog

&nbsp;   └── If bracket complete → injects Final-4 from top 3 + LB winner

```



---



\## 🔗 Button → Method Routing



| UI Button                    | Routed To                                |

| ---------------------------- | ---------------------------------------- |

| Generate Bracket             | `RaceController.GenerateBracket()`       |

| Match Winner 1 / 2           | `RaceController.SubmitWinner(matchId)`   |

| Advance Round                | `RaceController.AdvanceRound()`          |

| Generate Losers Bracket (RR) | `RaceController.GenerateLosersBracket()` |

| Save Session                 | `RaceSessionRepository.Save()`           |

| Load Session                 | `RaceSessionRepository.Load()`           |



---



\## 🔹 Engine Matrix



| Mode         | Adapter                   | Engine              | Match Type    | Source                 |

| ------------ | ------------------------- | ------------------- | ------------- | ---------------------- |

| Pro Ladder   | `ProLadderEngineAdapter`  | `MatchEngine`       | `LadderMatch` | `ProLadder.cs`         |

| Random Draw  | `RandomEngineAdapter`     | `RandomMatchEngine` | `RandomMatch` | `RandomBracket.cs`     |

| Round Robin  | `RoundRobinEngineAdapter` | `RoundRobinEngine`  | (Tuple)       | `RoundRobinEngine`     |

| Losers Brack | `RandomEngineAdapter`     | `RandomMatchEngine` | `RandomMatch` | `LosersBracketBuilder` |

| Final-4      | `ProLadderEngineAdapter`  | `MatchEngine`       | `LadderMatch` | `ProLadder.cs`         |



---



\## 🖊️ Setup Instructions (Dev)



\* Open in Visual Studio 2022+

\* Set startup project to `RCDragManagerProd`

\* `App.config` controls logging output

\* All UI is in Forms (WinForms)

\* No external DB needed (uses in-memory + JSON persistence)

\* Entry point: `Program.cs`



---



\## 📝 Future Work / Notes



\* MatchResultSave not yet wired into full export

\* Session save/load works, but lacks full race resume (planned)

\* Final injection paths (RR → LB → Pro) are working but not persisted

\* Future: double elimination support (blocked)

\* Legacy: MatchEngine is only used via adapter



