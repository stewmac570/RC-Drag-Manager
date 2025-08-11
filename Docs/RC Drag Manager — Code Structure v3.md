📄 RC Drag Manager — Code Structure v3.md

Updated: 04 Jul 2025 — Branch: feature/refactor‑bracket‑controller



1️⃣ Top‑Level Folder Map

bash

Copy

Edit

RCDragManagerProd/

├─ Controllers/          # thin orchestration layer (WinForms‑agnostic)

│   └─ RaceController.cs

├─ RaceEngines/          # adapters + factory + shared interface

│   ├─ IRaceEngine.cs

│   ├─ ProLadderEngineAdapter.cs

│   └─ RaceEngineFactory.cs

├─ ViewModels/           # DTOs consumed by the UI

│   ├─ PairingRow.cs

│   └─ WinnerRow.cs

├─ \[root]                # legacy engines, domain objects, WinForms UIs

│   ├─ \*WinForms\*  → Form1, LandingPageForm, SessionSetupForm, LoadSessionForm, dialogs…

│   ├─ \*Domain\*    → Driver.cs, Car.cs, RaceSession.cs, MatchResult.cs

│   ├─ \*Legacy\*    → MatchEngine.cs, RandomMatchEngine.cs, RoundRobinEngine.cs, LosersBracketEngine.cs

│   └─ \*Helpers\*   → RandomBracket.cs, RoundRobinRanker.cs, etc.

2️⃣ Namespaces \& Core Types

Namespace	Core Types	Purpose

RCDragManagerProd.Controllers	RaceController	Central coordinator between UI \& engines; raises events for UI refresh; owns current RaceSession.

RCDragManagerProd.RaceEngines	IRaceEngine, ProLadderEngineAdapter, RaceEngineFactory	Stable interface + adapters that wrap legacy engines; factory selects adapter.

RCDragManagerProd.ViewModels	PairingRow, WinnerRow	Simple immutable DTOs consumed by ListViews/labels.

(root)	Driver, Car, RaceSession, MatchEngine etc.	Existing domain models \& legacy logic.



📝 WinForms namespaces remain default; each form lives in its generated namespace.



3️⃣ Class Index (Concise)

Class	Namespace	Summary

RaceController	Controllers	Holds RaceSession, selected IRaceEngine; exposes events BracketRedrawn, NextMatchReady, WinnersUpdated, CanAdvanceChanged, CanPickWinnerChanged; public API GenerateBracket, SubmitWinner, AdvanceRound, Reset, SaveSession.

IRaceEngine	RaceEngines	Contract: LoadDrivers, GenerateBracket, GetMatches, GetRoundOrder, SetWinner, HasWinner, Reset.

ProLadderEngineAdapter	RaceEngines	Implements IRaceEngine by forwarding to legacy MatchEngine (NHRA ladder).

RaceEngineFactory	RaceEngines	Static Create(string raceType) returns correct adapter; currently supports “Pro Ladder”.

PairingRow	ViewModels	{int MatchId, string RoundLabel, string Driver1, string Driver2, bool IsHeader}.

WinnerRow	ViewModels	{int MatchId, string Winner, string Loser}.

MatchEngine	(root)	Legacy NHRA Pro‑Ladder generator/resolver. Remains unmodified.

RandomMatchEngine, RoundRobinEngine, LosersBracketEngine	(root)	Legacy engines pending adapters.

RandomBracket, RoundRobinRanker, LosersBracketBuilder	(root)	Helper generators \& ranking utilities.

All WinForms forms (Form1, LandingPageForm …)	(root)	UI presentation; now interact only via RaceController.



4️⃣ Key Dependencies \& Flow

text

Copy

Edit

\[WinForms UI]  ─┐        (buttons call)           (events raise)        ┌─►  ListViews / Labels

&nbsp;               │                                 BracketRedrawn ─────►┤

&nbsp;               ├─► RaceController ◄───────────── NextMatchReady ─────►┤  WinForms controls

Form1 / dialogs │                      CanAdvanceChanged ─────────────►┤

&nbsp;               │                      CanPickWinnerChanged ───────────►┘

&nbsp;               │

&nbsp;               └─► IRaceEngine  (via RaceEngineFactory)  ◄─┐

&nbsp;                                      ▲                   │ legacy engines

&nbsp;                                      │Adapts             │

&nbsp;                      ProLadderEngineAdapter  ────────────► MatchEngine (Pro‑Ladder)

✅ Only RaceController knows about IRaceEngine. UI never touches legacy engines directly.



5️⃣ Maintenance Notes

Adding a new race mode:



Create XYZEngineAdapter : IRaceEngine that wraps RandomMatchEngine / RoundRobinEngine.



Add a case to RaceEngineFactory.Create().



No UI changes required.



Persistence:



RaceController.SaveSession() is currently a stub.



Wire to RaceSessionRepository when schema stabilizes.



Refactor cleanup:



Delete unused helpers in Form1 (ProcessMatchWinner, engine refs, etc.) once fully transitioned.



Unit tests:



Mock IRaceEngine to test UI.



Test adapters separately for full coverage.



Threading:



Controller events fire on the calling thread.



If background logic is added, marshal events to UI thread (BeginInvoke()).



Naming:



Canonical WinForms controls: lblNext, btnWinner1, btnWinner2, btnNextRound, lvWinners.





