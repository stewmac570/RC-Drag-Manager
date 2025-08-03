# RC Drag Manager — Randomized Bracket Mode: Full Design Specification

---

## ✅ Feature Summary

This document defines the complete design, ruleset, architecture, logic flow, and class structure for the **Randomized Single-Elimination Bracket Mode** with Buybacks and Repeat Avoidance for the RC Drag Manager project.

This feature expands the existing bracket engines to support fully randomized blind draw brackets as an alternative to the existing NHRA Pro Ladder system.

---

## 🔧 Purpose

- Allow Race Director to run non-seeded random draw tournaments.
- Automatically handle any number of drivers (3 to 32).
- Support first-round BYEs using standard tournament expansion.
- Allow optional buybacks after Round 1 only.
- Prevent repeat matchups at all stages.
- Integrate cleanly into existing architecture without altering core NHRA code.
- Fully scalable for current and future session save/load system.

---

## 🔧 Final Locked Race Rules

| Rule | Description |
| ---- | ----------- |
| Bracket Type | Single-Elimination |
| Round 1 Pairing | Random Blind Draw |
| Byes | Auto-calculated based on next power of 2 |
| Buybacks | Allowed after Round 1 only |
| Repeat Pairings | Avoid repeat matchups entirely |
| Reseeding | Random draw after each round |
| Race Director Control | All rounds built only after prior round is complete |

---

## 🔧 Architecture Overview

### 📂 Class Responsibility Map

| File | Responsibility |
| ---- | -------------- |
| `ProLadder.cs` | NHRA official ladder system (unchanged) |
| `RandomBracket.cs` | **(NEW)** Full random bracket generator |
| `MatchEngine.cs` | Runs match state after bracket is generated |
| `MatchResult.cs` | Tracks match outcomes |
| `RaceSession.cs` | Stores full session state, driver list, match tree, and pairing history |
| `Form1.cs` | Controls user interface and race flow |

---

### 📂 New File: `RandomBracket.cs`

- Fully encapsulates the random bracket generation logic.
- Contains two core methods:
  - `GenerateFirstRound(List<DriverEntry> drivers)`
  - `GenerateNextRound(List<DriverEntry> activeDrivers, HashSet<(Guid, Guid)> pairingHistory)`
- Supports BYEs automatically.
- Prevents repeat matchups using pairing history tracking.
- Supports injection of buyback drivers after Round 1.

---

## 🔧 Logic Flow: RaceSession Lifecycle

### 🔬 Session Setup Phase

1️⃣ Race Director selects:
- Race Type → **Random Draw**

2️⃣ Driver roster is built via standard SessionSetupForm.

3️⃣ Session object is populated with full driver list.

---

### 🔬 Round 1 Bracket Generation

- Call: `RandomBracket.GenerateFirstRound(driverList)`
- Shuffle drivers randomly.
- Calculate:
  - `rounds = ceil(log2(N))`
  - `bracketSize = 2^rounds`
  - `byes = bracketSize - N`
- Assign BYEs to first X drivers in shuffled list.
- Pair remaining drivers randomly.
- Output full list of `Match` objects with correct `MatchId`, `Seed1`, `Seed2`, `FromMatch1`, `FromMatch2`.

---

### 🔬 Post-Round 1: Buyback Injection

- Race Director selects buyback drivers from Round 1 losers.
- Buybacks injected back into active pool for Round 2.

- Total Round 2 driver pool:  
  `Round1Winners + Buybacks`

---

### 🔬 Round 2+ Bracket Generation

- Call: `RandomBracket.GenerateNextRound(activeDrivers, pairingHistory)`
- All remaining drivers are randomly paired.
- Algorithm checks pairingHistory to avoid prior matchups.
- If conflict found:
  - Attempt pairing with alternate available driver.
  - If no valid pairings remain → forced repeat allowed (rare).
- BYEs applied if needed when odd number remains.

---

### 🔬 Pairing History

- Stored inside `RaceSession` object.
- Structure: `HashSet<(Guid driverA, Guid driverB)>`
- Both driver IDs stored in ordered tuple to normalize pairs.
- After each match completed, new pairs are recorded.

---

## 🔧 Full Data Model Changes

### 🔬 RaceSession.cs

```csharp
public class RaceSession
{
    // Existing fields...
    public List<Match> Matches { get; set; }
    public HashSet<(Guid, Guid)> PairingHistory { get; set; }  // NEW
    public List<Guid> BuybackEligible { get; set; }            // NEW
}
