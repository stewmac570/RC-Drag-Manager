---- DEV LOG PART 7 ----

ðŸ§ª Identified Next Tasks
Improve Generate Bracket button state flow across RR, LB, Finals.

Add popup alerts for race director at key phase transitions:

After RR complete

After LB winner selected

After Finals conclude
------------------------------------------------------------------------------
Dev Log â€“ Buybacks Flow & Losers Bracket Start Logic

Updated btnGenerateLosersBracket_Click in Form1.cs to:

Rename button text to "Buybacks".

Only open the driver selection dialog and store selected drivers â€” no automatic race start.

Added detailed logging for eligibility, selection, and storage.

Enabled Generate Bracket button after storing buybacks.

Added SetBuybackDrivers(List<Driver>) method in RaceController.cs to store selected buyback drivers in the session for later use.

Began restructuring Generate Bracket flow so that:

Clicking Generate Bracket after buybacks triggers Losers Bracket instead of restarting Round Robin.

Added IsInLosersBracketPhase property to RaceController.cs for phase detection.

Drafted StartLosersBracket() method in RaceController.cs to build matches from stored buybacks and switch engine to RandomEngineAdapter.

Encountered compilation issues due to:

Missing BuybackDrivers and TopDriversSnapshot properties in RaceSession.cs.

Nonexistent SetExternalMatches() method in RandomEngineAdapter (replaced with LoadDrivers() + InjectMatches()).

Event/method name mismatches (RedrawFullBracket â†’ BracketRedrawn).

Missing GenerateBracket(string) overload in RaceController.cs.

Added BuybackDrivers and TopDriversSnapshot properties to RaceSession.cs.

Created wrapper GenerateBracket(string) method in RaceController.cs to call the 2-argument version using session drivers.

Found that _session.Drivers was never set, causing â€œsession driver list is invalidâ€ log message â€” identified need to assign driver list during setup.

Status:
Buybacks dialog works without auto-starting race. Generate Bracket button re-enabled after buyback selection. Losers Bracket start wiring in progress but currently blocked by driver list assignment and session state handoff.
------------------------------------------------------------------------------
RC Drag Manager â€” Dev Log (Finals/LB gating, UI lists, scoring)
Date: 2025-08-10 (AEST)
Author: Stewart + assistant pairing

Flow & Gating
Added finals gate: finals no longer auto-inject on LB completion.

RaceController.cs: CanStartFinalsChanged event, _finalsPending flag, IsFinalsPending prop.

Form1.cs: enables Generate Bracket and shows â€œFinals Readyâ€ popup when LB ends.

Finals start only when Generate Bracket is pressed.

RaceController.cs: StartFinals() calls InjectFinal4Bracket() and drops gate.

Finals reveal sequencing fixed:

InjectFinal4Bracket() now reveals SF only; F is revealed after Generate Next Round.

Preserves revealed Losers rounds when injecting Finals (no list reset).

Losers Bracket (LB)
Start LB shows R1 immediately and pushes first pairing.

RaceController.cs: StartLosersBracket() loads drivers, injects matches, sets _inLosersPhase, reveals "Losers Bracket R1".

LB builder robustness:

Avoid BYE-vs-BYE in R1; no infinite loops; correct odd-carry to next round so LB Final always exists.

LosersBracketBuilder.cs: rebuilt Build(...) with paired iteration, odd-carry BYE match, consolidated id/r1 lists.

LB champion retrieval hardened:

RandomEngineAdapter.GetWinner() falls back to last round by order if â€œfinalâ€ label not found.

Unified UI Lists (left/right panes)
Current Round Pairings now shows all phases (RR â†’ LB â†’ Finals) with continuous M#:

Snapshot RR matches/order at RR completion (and fallback snapshot in StartLosersBracket()).

RaceController.cs: BuildCurrentBracketRows() aggregates RR snapshot, LB engine, and Finals engine; assigns MatchNumber = M1..M*.

PairingRow gained MatchNumber; Form1.RedrawFullBracket() uses it (with headers + logging).

Generate Next Round redraws go through the unified builder:

RaceController.cs: replaced AdvanceRound() to always BuildCurrentBracketRows() â†’ BracketRedrawn.

Match Winners ordering fixed & numbered continuously:

Form1.cs: new WinnersUpdated handler with global sort helper GetGlobalRoundOrder().

Explicit ranking: RR R1..Rn (100+n) â†’ LB R1..Rn (200+n) â†’ LB Final (299) â†’ SF (990) â†’ F (1000).

Buyback Eligibility
Corrected eligible list to be all RR entrants minus Top-3 (not just those appearing in standings).

RaceController.cs: GetEligibleBuybackDrivers() derives roster from RR matches; logs roster, Top-3, eligible names.

Sanity: 10 entries â†’ 7 eligible (verified in logs).

Finals Completion & UX
Added tournament completion event and simple OK-only popup (no auto reset/close).

RaceController.cs: RaceSummary DTO, TournamentCompleted event, summary emission in PushAdvanceState() when Final resolved.

Form1.cs: popup shows Event/Bracket/Winner/Runner-up/Matches; leaves session intact.

Added safe Reset() that clears engines/flags/UI when used manually.

Made GetMatch(int) null-safe after reset to prevent NRE.

Round Robin Scoring (auditability)
Exposed shared points schedule for R1/R2/R3:

RoundRobinRanker.cs: public static PointsForRound(string) and legacy GetPoints() delegating to it (with logging of schedule/unknown labels).

Added clear W-L scoreboard at RR completion:

RaceController.cs: LogRoundRobinScoreboard(rr) (names + W-L).

(Prepared) Detailed per-round/per-match scorecard helper (ready to enable next):

RaceController.cs: LogRoundRobinScorecardDetailed(rr) prints per-match points, round subtotals, final totals (not always on by default).

Logging & Diagnostics
Button state changes logged (Generate Bracket / Next Round / Generate Losers Bracket).

Lifecycle snapshots at key transitions: LB pre/post swap, finals gating, revealed rounds, row builds.

[ROWS] BUILD v2 entry logs active engines, snapshot counts, and revealed rounds every redraw.

LB builder logs R1 pairings, odd-carry creation, and total match counts.

Finals inject logs all generated matches with driver names.

Known Follow-ups / Nice-to-haves
Ensure btnNextRound is wired once (avoid duplicate â€œAdvanceRound completedâ€ logs if double-subscribed).

Optionally enable the detailed RR scorecard at RR completion (per-round subtotals) for race-day clarity.

Integrate MatchResult fully to eliminate any â€œWinner Mxâ€ placeholders in legacy paths.

Quick Acceptance (verified)
RR â†’ Buyback (correct eligible count) â†’ LB (R1..Final) â†’ Finals gate â†’ SF â†’ Next Round â†’ Final â†’ OK popup.

Left Current Round Pairings lists all rounds continuously, even after LB/Finals transitions.

Right Match Winners ordered RR â†’ LB R1..Final â†’ SF â†’ F with continuous M#.

No freezes on LB start; no auto-starting Finals; no Finals without user gating.
------------------------------------------------------------------------------
2025-08-12 â€” UI/UX + Round-Robin scoring + Random mode fixes
Features
Round-Robin score popup: Added RoundRobinScorecardLogger (new file) and wired it to show at RR completion. Includes per-round lines and a composite â€œScore = Pts + WinsÃ—0.01 + H2HÃ—0.001 + SoSÃ—0.000001â€ so ties are numerically clear.

In-app tie clarity: Popup rows now include driver names and show (Pts, W, H2H, SoS) for transparency.

Auto finals when no Buyback: If <2 eligible Buyback drivers (e.g., 4 racers), controller skips LB and injects Finals with the â€œwildcardâ€ 4th. User is notified.

Controller (RaceController.cs)
PushAdvanceState():

Logs RR standings, shows popup, snapshots RR, then:

If â‰¥2 Buyback eligible â†’ enable Buyback button.

Else â†’ auto-advance with wildcard and inject Final-4.

InjectFinal4Bracket(): Cleaned up; supports wildcard when LB absent; preserves LB rounds in left panel; reveals only SF initially.

BuildCurrentBracketRows():

Unified renderer now supports RandomEngineAdapter rounds (R2/R3 stay visible).

Fix for â€œself-matchâ€ display in LB Final: if engine collapses to champ vs champ, we recover (loser, winner) from MatchResult for display.

PushFullRefresh(): Uses unified builder (not the old BuildPairingRows()).

SubmitWinner(): Logs per-round RR scoring once a round fully resolves.

SaveSession(): Hardened (null-safe engines, results, revealed rounds). Writes only resolved/recorded matches; logs summary.

Default raceType: If UI passes blank, default to Round Robin and log.

Round-Robin ranking (RoundRobinMode/RoundRobinRanker.cs)
Loser derivation: If only a winner is stored (non-BYE), infer loser from the pairing.

SoS fix: Strength-of-Schedule sums final points of actual opponents and only for resolved matches.

Logging: [RR-PTS], [RR-OS], and pre/post sort tables for auditability.

Random mode (RaceEngines/RandomEngineAdapter.cs)
BYE fairness: Audits every round and redistributes BYEs so:

BYE goes to drivers with the fewest prior BYEs.

Avoids back-to-back BYEs for the same driver when possible.

Swaps recipients within the round as needed; detailed [RND-BYE] logs.

Next-round builder: Fair BYE selection in GenerateNextRoundFair() (for controller use).

GetWinner(): Replaced ^1 with Count-1 for legacy C# compatibility.

UI/UX
Current Round Pairings stays populated for Random mode across R2/R3.

Name visibility: Scorecard lines show driver names (not just IDs).

Finals gating: Finals button only enabled when LB completes or when we auto-advance.

Bugs fixed
Duplicate BYEs to the same driver across rounds (Random) â†’ fixed with fairness audit.

LB Final displayed as â€œX vs Xâ€ â†’ fixed by expanding from MatchResult.

Null-ref on Save & Close when engines/state were cleared â†’ fixed.

Files touched
Controllers/RaceController.cs

RoundRobinMode/RoundRobinRanker.cs

RoundRobinMode/RoundRobinScorecardLogger.cs (new)

RaceEngines/RandomEngineAdapter.cs

RaceEngines/RoundRobinEngineAdapter.cs (minor wiring)

ViewModels/PairingRow.cs (display support)

MatchResult.cs, RaceSession.cs, Form1*.cs (wiring + UI)

Docs/_PROJECT_STATUS_DEVLOG_FULL.md (updated)

Notes / Next
Optional: expose and call GenerateNextRoundFair() from the Random â€œGenerate Next Roundâ€ UI handler (if not already).

If desired, re-run BYE audit on each round reveal to keep fairness bullet-proof after edits/imports.

------------------------------------------------------------------------------
Repo + base project

Full repo recovery and cleanup finished; main stabilized, designers re-linked, namespace unified to RCDragManagerProd, and remote set.

Architecture + folder layout documented (UI, engines, repositories, domain).

Core features in place

Driver + car management, unified â€œAdd driver & carâ€ dialog, SQLite persistence, session setup (event name/date/type), and NHRA Pro Ladder engine (3â€“10 drivers) are all marked âœ….

Session creation / setup

Built out the entire SessionSetupForm: event details, class selection (Heads Up, Dial, Index), roster building, and live filtering; creates a RaceSession wired with correct DriverEntry objects.

Form1 workflow and race engine control

Race flow was rebuilt to be fully manual NHRA style: old auto-advance logic removed, BYEs kept but require manual advance, â€œGenerate Next Roundâ€ now the only way to move rounds, and match results are stored manually.

UI tweaks branch: added â€œSet Qual Timeâ€ below the driver list; fixed â€œEdit Driverâ€ wiring; standardized Form1 size; aligned bottom controls; tightened â€œGenerate Next Roundâ€ enable/disable rules; and clarified Save/Close in Quick Session.

Pro Ladder expansion

NHRA Pro Ladder expanded from 11 to 16 cars with correct seed/match mapping and round labels (R1, R2, SF, F). Compatible with existing save/load.

Save/Load system hardening

Session table auto-created when missing; insert/update flows fixed; debug tooling added; several save/load bugs identified and resolved. Outcome: save/load/delete now â€œ100% stable.â€

Runtime signals in logs (examples)

Bracket/UI rebuilds and finals completion are logged (headers/rows added, winner/runner-up, etc.), confirming the wiring during play.

Engine selection and bracket generation for Round Robin / Random Draw are logged during session runs.
------------------------------------------------------------------------------
Branch / scope

Branch: feature/installer-stabilization (pushed)

Commit: 9d8ca13 + .gitignore commit 7973960

Installer (Inno) â€“ final

Fixed arch line (x86 instead of ia32).

