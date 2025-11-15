---- DEV LOG PART 3 ----
Confirmed dial-in logic based on class type

Fixed and matched all control declarations in AddCarDialog.Designer.cs

Correctly wired all event handlers (btnOK_Click, ClassTypeChanged)

âœ… Fixed DriverManagerForm.cs
Fully cleaned and replaced btnAddCar_Click:

Now correctly uses new AddCarDialog() for new car creation

Calls dlg.NewCar and adds to selectedDriver.Cars

Fully cleaned and replaced btnEditCar_Click:

Retrieves selected car correctly

Calls AddCarDialog(car) and applies edited data

Eliminated all CS1503, CS1061, CS0103 errors

Standardized all method logic with proper in-scope variables

Final file compiles clean and matches your architecture

âœ… Build Status:
âœ”ï¸ All errors resolved
âœ”ï¸ All dialogs working
âœ”ï¸ Final rebuild successful
âœ”ï¸ Ready to check in

------------------------------
 Branch: feature/driver-stats
âœ… Added new Driver Stats button to DriverManagerForm (enabled on driver selection)

âœ… Created new WinForm: DriverStatsForm with separate .cs and .Designer.cs

âœ… UI matches LoadSessionForm styling (900Ã—600, top summary, detailed table view)

âœ… Top summary includes: Wins, Losses, Events Entered, Events Won

âœ… Unified ListView displays match history per session:

Columns: Event Name, Date, Round, Opponent, Result

Automatically shows "BYE" when driver had no opponent

âœ… Reads from RaceSession.SavedResults and DriverEntries

âœ… Uses MatchLookupHelper.cs to resolve RoundLabel by MatchId without active session

âœ… Fully supports both active and historical bracket inspection

âœ… All code modular, non-intrusive, and compatible with existing SQLite session structure

âœ… Branch pushed and ready for merge.
âœ… Feature tested and UI confirmed stable.

----------------------------------------

âœ… Feature: Random-Draw Bracket (and mixed Pro-Ladder fixes)
Status: COMPLETE & STABLE

Area	Work completed in this session
Random-draw first-round generator	â€¢ RandomBracket.GenerateFirstRound() incorporated.
â€¢ Shuffling, BYE allocation, correct MatchId assignment.
Random-draw round-by-round engine	â€¢ RandomMatchEngine created and wired.
â€¢ GenerateNextRound() avoids repeat pairings and auto-resolves BYEs.
btnGenerateBracket_Click	â€¢ Refactored: single isRandom flag.
â€¢ Random branch shuffles & loads randomEngine, adds revealedRounds.Add("R1").
â€¢ Pro-Ladder branch seeds, initialises MatchEngine, detects real first-round label (R1/SF/â€¦) and adds to revealedRounds.
RedrawFullBracket	â€¢ Guarantees lvPairings has columns (fix for blank list on Quick-Session forms).
â€¢ Separate logic for Pro-Ladder vs Random modes.
IsRandomMode() helper	â€¢ Returns true if race-type string contains â€œrandomâ€ (case-insensitive). Eliminates hard-coded "Randomized".
UpdateNextUp	â€¢ Re-written to use IsRandomMode.
â€¢ Handles null randomEngine and BYE names.
ProcessMatchWinner	â€¢ Re-written with IsRandomMode.
â€¢ Skips stat updates when loser is BYE.
â€¢ Supports both engines and session-launch path.
UpdateButtonStates	â€¢ Guard added: if randomEngine == null or revealedRounds.Count == 0 â†’ leave buttons disabled (fixes crash after Reset).
Reset Race	â€¢ randomEngine = null; added; full UI clear; buttons reset.
Columns added once	â€¢ RedrawFullBracket() initialises ListView columns when none exist (Create-Race-Session launch fix).
Winner list & â€œGenerate Next Roundâ€ state	â€¢ Correct enabling logic after each round; final round disables button.
Crash fixes	â€¢ Handled empty revealedRounds (Sequence contains no elements).
â€¢ Guarded against null engines in all methods.
UI feedback	â€¢ lblNext shows â€œUp Next: --â€ until bracket generated; â€œAll matches resolved.â€ at end.

Testing covered

Quick-Session â†’ Pro-Ladder (3 â€“ 16 drivers) âœ“

Quick-Session â†’ Random-Draw âœ“

Create-Race-Session â†’ Pro-Ladder âœ“

Create-Race-Session â†’ Random Draw (â€œRandom Drawâ€ label) âœ“

Reset Race on both modes âœ“

BYE auto-advancement & BYE stats skip âœ“

Next logical tasks

Persist RandomMatchEngine state to session save/load (future).

UI polish (column widths, scroll auto-scroll).

Extend Pro-Ladder templates 25 â€“ 32 drivers.

Feel free to paste the table (or adapt) into PROJECT_STATUS_DEVLOG_FULL.md under a new branch entry, e.g.:

arduino
Copy
Edit
### ðŸ”§ Branch: feature/random-draw-final-stabilise
âœ… Full random-draw bracket engine completed and integrated â€¦
â€”end of feature.

--------------------------------------------------------

Round-Robin feature â€” work completed in this chat

New engine files

RoundRobinEngine.cs â€“ generates 3 rounds of pairings (max 1 BYE/round, no rematches), tracks every result in DriverMatchResult.

RoundRobinRanker.cs â€“ weights points (R1 4.0 / R2 3.5 / R3 3.0), ranks by points â†’ wins â†’ H2H â†’ opp-strength â†’ random.

LosersBracketEngine.cs â€“ single-elimination bracket for all non-top-3 drivers; BYEs auto-advanced.

MatchEngine draft update

Added SessionType.RoundRobin and RacePhase enum.

FaÃ§ade logic routes calls to the active engine (Round-Robin â†’ Losers Bracket â†’ Pro Ladder) and detects completion.

Git actions

Created branch round-robin-engin.

Committed new files (RoundRobinEngine, RoundRobinRanker, LosersBracketEngine) with message
â€œMVP: add round-robin engines and rankerâ€.

Pushed branch to origin; ready for Pull Request.

Next-phase planning

Produced detailed MatchEngine_Refactor_Spec.md outlining extraction of ProLadderEngine, IMatchEngine interface, faÃ§ade design, UI wiring, unit-test matrix.

Provided prompt to start a fresh chat for the refactor.

This log entry captures all code additions, git commits, and the roadmap delivered in this session.

-------------------------------------------------

1. Initial Wiring of Round Robin Mode
Connected "Round Robin" as a valid race mode option in btnGenerateBracket_Click.

RoundRobinEngine was instantiated and LoadDrivers(...) + GenerateMatches() were called.

Round labels like "R1" were added to revealedRounds.

2. Initial RoundRobinEngine Setup
Refactored RoundRobinEngine to store match data as:

csharp
Copy
Edit
List<(Driver Driver1, Driver Driver2, string RoundLabel, int MatchId)> matches;
Implemented GenerateMatches() with logic for:

Avoiding rematches across rounds

Up to 3 rounds max

Assigning BYEs if odd number of drivers remain

3. Resolved Early Crashes
Fixed an issue where the app crashed when only one pairing existed due to:

Incorrect use of placeholder ResolveDrivers(LadderMatch) instead of correct overload.

Cleaned up UpdateEventWinnerStats() to handle all 3 race types with proper logic per engine.

4. Confirmed Matching Logic Runs Without Crash
Multiple iterations ensured the btnNextRound_Click and match processing doesnâ€™t crash.

âŒ Whatâ€™s Still NOT Working
ðŸ”´ Only One or Two Matches Appear
GenerateMatches() often results in just one pairing + one BYE, even with 6 drivers.

Expected: 3 unique pairings in Round 1 alone with 6 drivers, no BYEs.

ðŸ”´ Round Robin UI Not Fully Wired
Winner buttons on Form1 do not always respond when Round Robin mode is active.

The current ResolveDrivers(...) and match detection donâ€™t connect to UI correctly for Round Robin.

ðŸ”´ UI Hangs or Locks
Under certain states, GenerateMatches() loops infinitely if no unpaired opponents exist (bad exit condition).

Caused UI lockups during testing with higher driver counts or bad match logic.

âš ï¸ Problems Identified
Your current RoundRobinEngine.GenerateMatches() rotates drivers endlessly if no opponent is found.

Form1 UI isnâ€™t using the correct match list filtering logic when drawing Round Robin matches.

Conflicts between MatchEngine-based logic and new RoundRobinEngine logic are still unresolved in Form1.

ðŸ“Œ Next Step (As You Suggested)
We need to:

Start a clean chat

Upload Form1.cs, MatchEngine.cs, RoundRobinEngine.cs, RaceSession.cs, and others as needed

Get a precise implementation plan â€” no code, no guesses â€” just a surgical set of changes to get Round Robin fully functional and UI-integrated.

-----------------------------------------------------------------------------------

Round-Robin â€œsingle-pairâ€ bug â€“ what we fixed
Area	Change	Why it matters
Driver.cs	â€¢ Added static _nextRuntimeId + auto-assign logic so every Driver gets a unique runtime ID unless one is loaded from storage.	Round-Robin pairing relied on IDs; duplicates caused missing pairings.
Form1.cs â€“ Generate Bracket	â€¢ Unified UI refresh after generation.
â€¢ Added full Round-Robin branch.	All first-round pairs now appear.
Form1.cs â€“ ProcessMatchWinner	â€¢ New Round-Robin branch.
â€¢ Correct 2-param call for Randomized engine.
â€¢ Shared UI refresh.	Winner buttons now record results in every race type.
Form1.cs â€“ GetNextHiddenRound & UpdateButtonStates	â€¢ Both methods now query the active engine (Pro Ladder / Randomized / Round Robin).	Generate Next Round enables exactly when it should.
RoundRobinEngine.cs	â€¢ Re-implemented GenerateMatches() with â€œcircle methodâ€ scheduling (3 rounds, no rematches, â‰¤1 BYE/round).	Fixed the â€œtwo drivers disappear after R1â€ issue.
Form1.cs â€“ Save & Close logic	â€¢ Replaced single Pro-Ladder loop with three branches that pull results from the correct engine.
â€¢ Uses your MatchResultSave model.	Stopped NullReferenceException when saving Round-Robin or Randomized sessions.
Misc. UI refresh	Ensured every engine change calls RedrawFullBracket, UpdateNextUp, UpdateWinnersList, and UpdateButtonStates.	Keeps the interface in sync after any action.

Outcome

Round-Robin sessions now:

generate complete pairings for 3 rounds,

let you pick winners,

unlock Generate Next Round when all current matches are resolved,

save/restore correctly without crashing.

Pro Ladder and Randomized modes continue to work exactly as before.

----------------------------------------------------------------

Branch: feature/phase2-task1-ranker
Date: 2025-06-22
Developer: Stewart McMillan
Focus: Round Robin support + universal BYE protection for match UI

ðŸŽ¯ Purpose of Task
Resolve critical UX/UI bug where winner buttons remained enabled when a driver was paired against a BYE in all race modes â€” including Round Robin, Randomized Bracket, and Pro Ladder.

Previously, if a race contained an odd number of drivers, the BYE pairing could:

Cause crashes when the user selected the BYE driver

Allow invalid match resolutions

Mismatch display logic between "Next up", button labels, and internal state

ðŸ”§ Fixes and Features Implemented
1ï¸âƒ£ Fixed Button Enable Logic for BYE
Updated UpdateNextUp() (in Form1.cs) to:

Automatically disable winner buttons for BYE drivers

Enable only the driver that is racing against BYE

Fully disable both buttons if both drivers are null or the match is resolved

csharp
Copy
Edit
btnWinner1.Enabled = d1 != null && d1.Name != "BYE";
btnWinner2.Enabled = d2 != null && d2.Name != "BYE";
2ï¸âƒ£ Updated All Race Modes (Pro Ladder, Random, Round Robin)
Ensured button state logic is consistent across all formats:

ðŸ” Randomized Bracket Mode

ðŸ”ƒ Round Robin Engine

ðŸ NHRA-style Pro Ladder

The UpdateNextUp() method now contains separate logic branches per race mode, but uses the same rule for disabling invalid matches.

3ï¸âƒ£ Resolved Crashes in UpdateDriverStats()
Bug: pressing a button for a BYE match caused a null reference exception in UpdateDriverStats() because winner or loser was null.

Fix: added a safe check at the top of the method:

csharp
Copy
