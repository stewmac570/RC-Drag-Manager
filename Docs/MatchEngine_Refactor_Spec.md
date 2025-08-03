MatchEngine_Refactor_Spec.md
markdown
Copy
Edit
# RC Drag Manager — Match Engine Modularisation (MVP v2.0)

## 1 . Purpose
Refactor the monolithic `MatchEngine` into a façade that delegates race-logic
to pluggable engines.  
This enables:
* Clean separation of rule-sets (Pro Ladder, Round Robin, Losers Bracket)
* Unit-test isolation per engine
* Easier future additions (e.g. Chicago Shootout, Time-Attack)

---

## 2 . High-Level Flow

┌───────────────┐
│ MatchEngine │ (façade)
└──────┬────────┘
│ chooses by SessionType / Phase
┌──────▼──────┐ ┌──────────────┐ ┌──────────────────┐
│ProLadderEng │ │RoundRobinEng │ │LosersBracketEng │
│ (exists) │ │ (new) │ │ (new) │
└─────────────┘ └──────────────┘ └──────────────────┘

pgsql
Copy
Edit

---

## 3 . Files & Responsibilities

| File | Responsibility | Key Methods |
|------|----------------|-------------|
| **`IMatchEngine.cs`** | Common contract all engines implement | `GetCurrentMatch()`, `SetWinner(..)`, `Advance()`, `bool IsComplete` |
| **`ProLadderEngine.cs`** | *Extract* existing ladder code from `MatchEngine` | same as interface |
| **`RoundRobinEngine.cs`** | Already created – no change | – |
| **`LosersBracketEngine.cs`** | Already created – no change | – |
| **`MatchEngine.cs`** (new façade) | Holds current `Phase`, selects active engine, routes calls | • ctor(SessionType, drivers) <br>• `Phase` enum updates <br>• wrapper methods |
| **`RacePhase.cs`** | Enum: `RoundRobin`, `LosersBracket`, `ProLadder`, `Complete` | – |

---

## 4 . Interface Definition

```csharp
public interface IMatchEngine
{
    object GetCurrentMatch();               // engine-specific DTO
    void   SetWinner(Guid a, Guid? b, Guid winner);
    void   Advance();
    bool   IsComplete { get; }
}
All engines implement this; UI never reaches into internal collections.

5 . MatchEngine Façade Logic
Session selection

SessionType.ProLadder → start in RacePhase.ProLadder

SessionType.RoundRobin → start in RacePhase.RoundRobin

Phase transitions

When RoundRobinEngine.IsComplete →
– rank drivers → pick non-Top-3 → run LosersBracketEngine
– inject winner + Top-3 into ProLadderEngine → RacePhase.ProLadder

When ProLadderEngine.IsComplete → RacePhase.Complete

Public façade methods
Delegate to _activeEngine (private field) and update Phase.

6 . BYE Handling
csharp
Copy
Edit
public static class DriverConstants
{
    public static readonly Driver Bye = new() { Name = "BYE", Seed = 0 };
}
All engines reference the same instance ⇒ equality checks consistent.

7 . Unit-Test Matrix
Engine	Test	Expected
RoundRobin	3 rounds, no repeat pairings	unique pairs
RoundRobin	Odd count => ≤1 BYE/round	assert count
Ranker	Points & tie-break order	deterministic ranks
LosersBracket	Bracket size power-of-two with BYEs	winner reachable
Façade	Full simulated flow (6 drivers)	final Phase == Complete

8 . Git Workflow
bash
Copy
Edit
git checkout -b match-engine-refactor
# 1. ADD IMatchEngine + ProLadderEngine extraction
# 2. ADD façade MatchEngine
# 3. Adapt RoundRobinEngine & LosersBracketEngine to interface
# 4. Update Form1 to call façade only
# 5. Pass unit tests
git push -u origin match-engine-refactor
9 . UI Wiring (Form1)
Keep existing visual layout.

Tabs: bind visibility to matchEngine.Phase.

btnNextRound_Click → calls matchEngine.Advance() only.

10 . Acceptance Criteria
✅ Pro Ladder flow unchanged for existing sessions.

✅ Round-Robin session runs 3 rounds, ranks, pushes Top-4 to ladder.

✅ No UI crashes when switching tabs.

✅ All new code 100 % covered by unit tests.