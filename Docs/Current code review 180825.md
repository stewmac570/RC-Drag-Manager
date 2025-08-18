Current code review 18/08/25


Got it. Pass A — review only. Here’s the quick read on Program.cs.

Program.cs — Review Notes (no code changes)

Touches/depends on

Config/AppSettings (Load(), EnableLogging, LogFilePath)

Logging/Logger (Logger.Log(...))

Repositories/DatabaseInitializer (InitializeDatabase(connStr))

UI/Forms/LandingForm (expects LandingForm(string connectionString))

Flow sanity

Loads settings before logging (good).

Global exception hooks set (good).

Ensures %APPDATA%\RC_Drag_Manager\race_data.db exists, then InitializeDatabase (assumes it creates schema).

Starts LandingForm with the connection string.

Risks / edge cases

If AppSettings.Load() throws, app dies before hooks/logging. (We’ll wrap in try/catch in Pass B.)

ShowFatal uses AppSettings.LogFilePath; could be null/empty. (Guard in Pass B.)

No ApplicationExit log hook. (Nice to add; low risk.)

The empty DB file is created with using var _ = new FileStream(..., CreateNew, ...). Fine, but if the file exists but is locked elsewhere, InitializeDatabase could fail; we’ll just log better.

No version banner log (optional nice-to-have).

Style/cleanup targets for Pass B

Guarded settings load with fallback.

Add Application.ApplicationExit += ….

Optional: version string in startup log.

Minor: normalize the temp variable name (from _) for readability if you prefer.

Ensure Logger respects the “logging off by default” rule via AppSettings.EnableLogging (we will confirm in AppSettings.cs).

Verdict: Solid baseline; only safety/guard polish needed.

--------------------------------------------------------

Pass A — review only. Here’s the read on Config/AppSettings.cs.

AppSettings.cs — Review Notes (no code changes)

Purpose

Persist user settings to %APPDATA%\RC_Drag_Manager\appsettings.json.

Toggle EnableLogging (DEBUG: true, RELEASE: false) — matches your “logging OFF by default for release” requirement.

Provide LogFilePath under the same app folder.

Dependencies

None external to config; used by Program.cs and likely Logger.

Flow

Load() ensures app folder, reads JSON if present, else writes defaults via Save().

Fail-safe: any exception → reverts to new Model() defaults (good).

Risks / edge cases

Silent failures in Save() (exceptions swallowed). Fine for startup safety, but we may want to emit a best-effort log later (guarded) in Pass B.

LogFilePath does Directory.CreateDirectory(AppFolder) on every get — harmless but a bit chatty.

No schema/versioning for settings. If we add fields later, deserialization will default missing fields; acceptable.

Thread-safety: not locked. Probably fine (settings set from UI rarely), but if toggled during runtime, concurrent reads/writes could race (low risk).

Cleanup targets for Pass B

Add a single source of truth for app paths to reduce duplication:

public static string AppDataDirectory => AppFolder;

(Optional) public static string DatabasePath(string dbFileName) or public static string DbFilePath so Program.cs doesn’t recompose.

Consider a guarded helper to attempt logging an error when Save() fails only if logging is already enabled (no hard dependency).

Minor: add JsonSerializerOptions with AllowTrailingCommas = true for resilience (optional).

Verdict: Solid. Meets the release-logging-off rule. Only minor polish recommended.

-------------------------------------------------------------------
Pass A — review only. Here’s the read on Logging/Logger.cs.

Logger.cs — Review Notes (no code changes)

Dependencies

Config.AppSettings (EnableLogging, LogFilePath).

Behavior

Static ctor resolves _logPath and ensures folder exists.

Enabled checks AppSettings.EnableLogging on every call (good for live toggling).

Log(string) appends timestamped line; errors are swallowed.

Convenience: LogError, LogFatal prefix tags.

Risks / edge cases

No rotation: log can grow unbounded.

No thread safety: multiple threads could interleave writes (WinForms mostly fine, but background tasks or async events could collide).

Silent failures: if write fails (locked, disk full), logs vanish with no fallback.

Inconsistent levels: base Log() has no level; only the two helpers add tags.

Time format: local time only; hard to correlate across systems (maybe fine).

Cleanup targets for Pass B

Add a size cap + rotate (e.g., 5 MB roll w/ .1, .2).

Add a simple lock around writes (lightweight object _sync).

Add LogException(Exception ex, string context = null) to format stack traces consistently.

Include level + source format, e.g., [INFO], [DEBUG], [ERROR].

Optionally switch to UTC or include offset K in timestamp yyyy-MM-dd HH:mm:ssK.

Consider a fallback to %TEMP%\rcdrag_fallback.log on IO failure (last resort).

Verdict: Works as-is; minimal, safe. Pass B will harden it (rotation + lock + exception helper) without touching call sites.

------------------------------------------------------

Pass A — review only. Reading Domain/Drivers.cs.

Driver.cs — Review Notes (no code changes)

Purpose & fields

Identity: Id (int), runtime-assigned via static counter; can be explicitly set.

Core: Name, QualTime?, Seed?

Extras: Notes, stats (TotalWins, TotalLosses, EventsEntered, EventsWon), State (string), Cars : List<Car>

Dependencies

Domain.Car (list of cars)

Engines/UI likely use: Name, QualTime, Seed, Id

What’s good

Thread-safe runtime ID generation (Interlocked.Increment)

Allows explicit Id assignment (repository restore)

Defaults for collections (Cars = new())

Risks / edge cases

ID counter drift/collisions

When setting an explicit Id (e.g., loading from DB), _nextRuntimeId is not raised.

Later, auto-created drivers may reuse already-taken small IDs.

Example: DB has Id=25, _nextRuntimeId still 1 → app creates Driver Id=2,3,… colliding with persisted ones.

Id setter reset rule

Special-case ignores 0, but still permits changing a non-zero Id to another non-zero later (could break references).

Nullability & validation

Name can be null (UI display risks).

QualTime can be negative/NaN if set externally (seeding logic risk).

Seed may be set out-of-range for bracket size.

Equality semantics

No Equals/GetHashCode. If dictionaries/sets use Driver, identity is reference-based → duplicates if same Id loaded twice.

Stats integrity

Counters are freeform; no guard against negatives.

State field

Free text; if used for filtering, case/format inconsistencies likely.

Targets for Pass B (safe cleanup)

Stabilize identity

Update _nextRuntimeId on explicit Id set: if value > _nextRuntimeId, atomically lift the counter to value.

Optionally make Id setter internal (repo-only) and expose SetIdFromStore(int) — only if call sites allow; otherwise keep public but guarded.

Add IEquatable<Driver> and override Equals/GetHashCode by Id (non-breaking to callers that don’t rely on reference equality).

Guards

Ensure Name fallback: expose DisplayName => Name ?? $"Driver #{Id}".

Validate setters (or add helpers): prevent negative QualTime; clamp stats to >= 0.

Add helper: bool HasQualTime => QualTime.HasValue;

Logging (light)

Optional: trace when Id is explicitly set and when the counter is advanced (behind logging flag).

Minor tidy

Consider HomeState or Region naming (only if not widely referenced).

Seal the class if not inherited anywhere (check forms/engines first).

Verdict: Works, but identity management is fragile. We’ll harden Id handling in Pass B without touching consumers.

------------------------------------------------------

Pass A — review only. Reading Domain/Car.cs.

Car.cs — Review Notes (no code changes)

Purpose & fields

Identity: Id (int)

Legacy alias: CarID ⇄ Id (kept for old callers)

FK: DriverId

Data: CarName, ClassType, DefaultDialIn?

Dependencies

Domain.Driver via DriverId (no nav prop)

What’s good

CarID alias preserves legacy code without breaking new code.

Nullable DefaultDialIn is sensible.

Risks / edge cases

No guard on DriverId (0/default could slip in).

CarName, ClassType can be null/empty (UI lists, filters).

DefaultDialIn can be negative/NaN if set from bad input.

No equality overrides; collections keyed by Car risk duplicates by reference.

If both Id and CarID get set in different paths, they still map to the same backing (Id) so safe—just note for consistency.

Targets for Pass B (safe cleanup)

Input guards/helpers:

Clamp DefaultDialIn to >= 0 or validate on set.

Helper: HasDialIn => DefaultDialIn.HasValue.

Display helpers:

ToString() returning CarName ?? $"Car #{Id}".

Identity consistency:

Consider IEquatable<Car> + Equals/GetHashCode on Id.

Optional polish:

If legacy callers are all updated later, deprecate CarID with [Obsolete] (not now).

Verdict: Fine; small safety/quality improvements in Pass B.

---------------------------------------------------------------

Pass A — review only. Reading Domain/MatchResult.cs.

MatchResult.cs — Review Notes (no code changes)

Purpose

Central store mapping matchId -> (Winner, Loser) to eliminate “Winner Mx” placeholders elsewhere.

API

SetWinner(matchId, winner, loser)

GetWinner/GetLoser

HasResult / IsMatchResolved (duplicates)

ClearFromMatch(matchId) → removes all results where key >= matchId

IsTournamentComplete(bracketMatches) → checks final "F" resolved

GetAllPairings() → returns normalized (minId,maxId) pairs seen so far

Clear()

Dependencies

Domain.Driver

Domain.ProLadder.LadderMatch (RoundLabel, MatchId)

What’s good

Pair normalization in GetAllPairings avoids duplicates.

ClearFromMatch supports rollback/re-sim from a given matchId.

Simple, readable API.

Risks / edge cases

Null guards

IsTournamentComplete does not guard bracketMatches == null or empty.

SetWinner accepts null winner/loser (later logic may assume non-null).

Duplicate methods

HasResult and IsMatchResolved are identical.

Thread-safety

No locking around results; if any background ops log or mutate, risk race (likely fine in WinForms single-thread UI, but controller callbacks could be async).

ClearFromMatch semantics

Assumes match IDs are monotonic w.r.t. progression. If ProLadder IDs are non-sequential per round, clearing >= id might remove unrelated earlier-round results (might be intended, but confirm).

If IDs are reused across different brackets (e.g., losers bracket offset 1000), behavior is okay but be explicit.

Immutability / reference drift

Stores references to Driver. If a Driver.Id is later changed (allowed by current Driver.Id setter), pairing history could become inconsistent.

Hard-coded round label

"F" is fine if canonical; consider constants/enums to avoid typos.

Targets for Pass B (safe cleanup)

Add guards:

IsTournamentComplete: if (bracketMatches == null || bracketMatches.Count == 0) return false;

SetWinner: throw ArgumentNullException or at least ignore if winner==null.

Remove duplication: drop IsMatchResolved or make it forward to HasResult.

Mark results as readonly and rename _results for clarity.

Add TryGetWinner/Loser pattern to avoid double dictionary lookups.

Optional: introduce a small struct MatchOutcome { Driver Winner; Driver Loser; } to improve readability.

Optional: add ClearExact(matchId) separate from ClearFromMatch.

Optional: light logging when setting/removing results (behind flag).

Verdict: Works; needs minor null/duplication cleanup and clearer rollback semantics.

----------------------------------------------------------

Pass A — review only. Reading Domain/RaceSession.cs.

RaceSession.cs — Review Notes (no code changes)

Purpose

Aggregate for a race session: event meta, drivers, bracket state, history, saved results, and mode-specific match lists.

Dependencies

RCDragManagerProd.Logging.Logger

RCDragManagerProd.RandomMode.RandomMatch
⚠️ RoundRobinMatch is listed here but your using is RandomMode; in the tree it’s under RoundRobinMode/RoundRobinMatch.cs. So using RCDragManagerProd.RandomMode; is wrong for that type.

Structure

Core fields: EventName, EventDate, RaceType, ClassType, FixedDialIn?

Collections:

DriverEntries (denormalized snapshot of driver+car at registration)

Drivers (live Domain.Driver list)

Matches (RandomMatch list; Random-mode specific)

RoundRobinMatches (RoundRobinMatch list; RR-mode specific)

SavedResults (flat MatchResultSave ids)

SavedRevealedRounds (UI-ish)

PairingHistory (HashSet of normalized driver-id pairs)

BuybackDrivers, TopDriversSnapshot (transient)

Good

Keeps pairing-history with normalized (min,max) ids.

Separation between DriverEntries (entry snapshot) and live Drivers.

Risks / smells

Cross-mode coupling in a single domain model

Session contains Random and RoundRobin-specific lists. Hard to persist cleanly; increases null/unused fields depending on mode.

UI-state leakage

SavedRevealedRounds, TopDriversSnapshot, possibly BuybackDrivers feel like controller/UI state, not core domain.

Constructor logging noise

Logs every instantiation; can spam logs when loading or cloning sessions.

Stringly-typed RaceType

Susceptible to typos; better as enum in Pass B (without breaking serialization).

No guards / nullability

EventName, ClassType can be null; downstream UI may assume non-null.

FixedDialIn negative values not prevented.

Id semantics

Id plain int; ensure repository sets it and avoids collisions (we’ll check in RaceSessionRepository.cs).

MatchResultSave duplication

Separate class defined here while a ViewModels/MatchResultSave.cs also exists (per tree). Risk of duplicate types or namespace confusion.

Targets for Pass B (safe cleanup)

Fix the wrong using for RoundRobinMatch (point to RoundRobinMode).

Consider making UI-only state transient (controller-level) or move to a RaceSessionRuntime wrapper; if that’s too invasive, at least mark with comments.

Replace RaceType with an enum RaceTypeKind while keeping a string converter for persistence (non-breaking).

Add minimal guards (e.g., clamp negatives on FixedDialIn, ensure non-null lists).

Tone down constructor logging (switch to trace or remove).

Ensure there’s only one MatchResultSave type (prefer the ViewModel or move this one to Domain and delete the other later).

------------------------------------------------------

Pass A — review only. Reading Domain/ProLadder.cs.

ProLadder.cs — Review Notes (no code changes)

Purpose

Hard-coded NHRA-style ladder definitions. GetLadder(fieldSize) switches to specific builders returning List<LadderMatch>.

API/Model

LadderMatch { MatchId, Seed1?, Seed2?, FromMatch1?, FromMatch2?, RoundLabel }

BYE represented via Seed2 = 0. FromMatch* = null when sourced from seeds.

Good

Clear structure per field size.

Uses "R1", "R2", "R3", "SF", "F" consistently (mostly).

Covers sizes 3–24.

Issues / Risks

Visibility inconsistency

Some builders are private static (3–18), but GetLadder19…24() are public static. All should be private; only GetLadder(int) should be public.

Coverage gap (spec says 3–32)

Missing ladders for 25–32. GetLadder falls back to new List<LadderMatch>() → silent failure downstream.

Potential NHRA rule deviations (needs validation pass)

Several brackets (e.g., 10, 18, 20) award BYEs in later rounds (R2, SF) instead of traditional top-seed R1 byes. This may diverge from strict NHRA Pro Ladder charts. We must cross-check before any change.

Comment inaccuracies / noise

Example: GetLadder19() R1 MatchId=10 has // BYE comment, but both seeds 7 vs 14 are present (not a bye). Misleading.

BYE representation not standardized

Uses Seed2 = 0 in some rounds; other places rely solely on FromMatch*. Ensure engines uniformly treat Seed2 = 0 as auto-advance for Seed1/FromMatch1.

Round labels stringly-typed

Typos would break logic/UI. Consider constants/enum later.

Formatting inconsistencies

Mixed indent styles, spacing (e.g., Seed1 =6), stray blank lines.

No guardrail in GetLadder

Silent empty list for unsupported sizes; better to log/throw in Pass B to avoid “Winner Mx” ghosts.

Targets for Pass B (safe cleanup)

Make all builder methods private static.

Add logging/guard in GetLadder(int) when size unsupported (no logic change; just warn).

Normalize BYE comments and ensure consistent BYE encoding (Seed2 = 0).

Create static class Round constants: R1/R2/R3/SF/F (no behavior change).

Formatting pass only; do not alter pairing logic until we verify against NHRA charts.

Add TODO: implement 25–32 ladders (after verification).

Open check (before any edits)

Confirm MatchEngine/Adapters expect RoundLabel values exactly ("SF", "F") and treat Seed2=0 as BYE for Seed1.

-------------------------------------------------------

Pass A — review only. Reading RaceEngines/IRaceEngine.cs.

IRaceEngine.cs — Review Notes (no code changes)

Purpose

Contract for engines (Pro Ladder / Random / Round Robin) + neutral DTO EngineMatch for UI/controllers.

Usings / deps

Duplicate usings (System.Collections.Generic & RCDragManagerProd.Domain appear twice).

RCDragManagerProd.ViewModels is referenced but not used here. Drop it in cleanup.

Interface

LoadDrivers(List<Driver>), GenerateBracket(), Reset()

GetMatches() : IReadOnlyList<EngineMatch>

GetRoundOrder() : IReadOnlyList<string>

SetWinner(int matchId, Driver winner), HasWinner(int matchId)

DTO: EngineMatch

MatchId, Driver1, Driver2, RoundLabel, FromMatch1, FromMatch2, HasResult

Good

UI decoupled from engine-specific types.

Round path (FromMatch*) included for downstream resolution.

Sealed DTO (nice).

Risks / edge cases

SetWinner has no loser parameter; engines must infer loser from the two drivers. That’s fine but differs from Domain.MatchResult API which stores both.

BYE handling is implicit. If an engine assigns Driver2 == null (or HasResult=true), UI must cope. Confirm how engines mark BYE auto-wins.

RoundLabel is stringly-typed; typos will break UI filtering. (We’ll unify via constants later.)

No method to expose current round or “next match” directly; controller likely computes it. Leave as-is for now.

Targets for Pass B (safe cleanup)

Remove duplicate/useless usings; keep only:

using System.Collections.Generic;
using RCDragManagerProd.Domain;


Add XML docs on members (short).

Consider static class Round constants used across engines and UI for "R1","R2","R3","SF","F" (no behavioral change).

Optional: add public bool IsBye => Driver2 == null; to EngineMatch (only if no widespread impact; otherwise the controller can compute it).

Verdict: Interface shape is fine. Cleanup is mostly imports + docs + round constants (non-breaking).

------------------------------------------------------

Pass A — review only. Reading RaceEngines/MatchEngine.cs.

MatchEngine.cs — Review Notes (no code changes)

Usings / noise

Duplicates: System, Collections.Generic, Linq each appear twice.

using RCDragManagerProd.ViewModels; not used.

Role

Core NHRA seeding + bracket resolver:

Sorts by QualTime (timed first, fastest→slowest), then Name.

Assigns Seed = i+1 (overwrites any existing seeds).

Builds bracketMatches via ProLadder.GetLadder(count).

Maps seeds → drivers.

Stores results in MatchResult.

API

Initialize(List<Driver>)

SetWinner(matchId, winner[, loser]) → forwards to MatchResult

GetBracketMatches(), IsTournamentComplete()

ResolveDriversForMatch(LadderMatch) → returns (Driver, Driver); substitutes new Driver { Name="BYE" } for nulls.

ResolveDriver(seed?, fromMatch?)

RewindToMatchRound(matchId), GetAllDrivers()

Good

Seeding logic is deterministic and stable.

Round resolution uses either explicit seed or winner from prior match.

Logging driver counts (timed vs no-time).

Issues / risks

BYE auto-win not implemented

Architecture says: “Handles BYE auto-wins for Seed1 when applicable.”

Current code creates dummy Driver { Name="BYE" } and does not auto-advance the real driver. That placeholder can leak into UI, stats, pairing history.

Dead field

_byeGiven is never used.

Seed overwrite

Initialize resets .Seed for all drivers. If callers rely on pre-set seeds, they’ll be lost. (Probably OK given QualTime seeding is source of truth, but worth noting.)

Null / empty ladder

If ProLadder.GetLadder(n) returns empty (unsupported sizes), engine proceeds silently. Downstream UI may render nothing.

Logging gaps

Only one log in Initialize. No logs for SetWinner, RewindToMatchRound, or per-match resolution (helpful for debugging).

Accessors

GetAllDrivers() returns mutable List<Driver> (external mutation risk).

Targets for Pass B (safe cleanup)

Remove duplicate usings; drop unused ViewModels import.

Implement BYE auto-advance (no UI involvement):

When match.Seed2 == 0 (or resolved d2 == null), immediately set winner to d1 in Results if not already done; record once (track with _byeGiven).

Do not construct BYE Driver objects; let controller/UI detect bye by null.

Use _byeGiven to avoid double-processing BYEs.

Add logs:

Initialize (seed order summary), SetWinner (id + names), RewindToMatchRound, and when BYE auto-advances.

Guard unsupported ladders:

If bracketMatches.Count == 0, log a clear warning with allDrivers.Count.

Return IReadOnlyList<Driver> from GetAllDrivers() (or at least AsReadOnly()), if call sites allow; otherwise leave and add a TODO.

Verdict: Core shape is fine; BYE handling and minor hygiene needed in cleanup.

-------------------------------------------------------

Pass A — review only. Reading RaceEngines/ProLadderEngineAdapter.cs.

ProLadderEngineAdapter.cs — Review Notes (no code changes)

Usings

using RCDragManagerProd; not needed.

using RCDragManagerProd.ViewModels; not used.

using RCDragManagerProd.Logging; imported but not used.

Role

Wraps MatchEngine to satisfy IRaceEngine.

Good

Clear lifecycle: LoadDrivers → GenerateBracket → GetMatches/SetWinner/....

GetRoundOrder() sorts via LabelToIndex (covers R1–R5, QF, SF, F).

Reset() reinstantiates engine (clean state).

Issues / Risks

Duplicate resolution logic vs engine

Adapter re-resolves drivers by seed or prior match (MapToDto) instead of delegating to MatchEngine.ResolveDriversForMatch. This duplicates logic and can drift.

BYE placeholders leak

MapToDto converts nulls to new Driver { Name = "BYE" }. That pollutes downstream pairing history/stats/UI. We want null to signal BYE and let the engine auto-advance.

Dead filter

GetMatches() drops “BYE-BYE” via Where(m => !(m.Driver1 == null && m.Driver2 == null)), but MapToDto never returns null drivers (it replaces with “BYE” objects), so the filter never triggers.

Seed lookup inefficiency

FirstOrDefault(d => d.Seed == src.Seed1) is O(n) each call; MatchEngine already holds a seedMap. Use engine’s resolution to avoid N² behavior when rendering lists.

Stray content artifacts

Comments like :contentReference[oaicite:…] should be removed.

No logging

Adapter never logs GenerateBracket, GetMatches count, or SetWinner—handy for tracing.

Targets for Pass B (safe cleanup)

Remove unused usings and stray oaicite artifacts.

Delegate resolution to engine:

Replace MapToDto contents with _engine.ResolveDriversForMatch(src) to get (d1,d2) and do not fabricate “BYE” drivers.

Keep BYE as null and let engine handle auto-wins (we’ll implement that in MatchEngine).

Add light logs: bracket generated, totals, winners set.

Keep LabelToIndex as-is; later we can centralize round constants.

Verdict: Works, but leaks BYE placeholders and duplicates engine logic. Cleanup is straightforward and non-breaking.

------------------------------------------------------

Pass A — review only. Reading RaceEngines/RandomEngineAdapter.cs.

RandomEngineAdapter.cs — Review Notes (no code changes)

Usings

RCDragManagerProd.ViewModels not used → drop in cleanup.

Others OK.

Role

Bridges RandomMode.RandomMatchEngine to IRaceEngine.

Adds fair BYE policy across rounds (no repeats / no consecutive BYEs if avoidable).

Provides GenerateNextRoundFair() to build subsequent randomized rounds.

Good

Clear adapter lifecycle (Load → Generate → GetMatches/SetWinner → Reset).

BYE accounting _byeCount + _lastByeRecipient + schedule-rescan keeps fairness consistent.

InjectMatches() makes it easy to replace/append a schedule.

GetWinner() tries “Final” by label, falls back to last round in order.

Logs at useful points.

Risks / edge cases

Round labels

Uses free-text labels (“Round N”, “Final”) while other engines use "R1"/"SF"/"F". Mixed labeling complicates controllers/UI. We’ll align later (without breaking behavior).

GetWinner heuristic

If no “Final” and no round order, returns null. That’s fine, but log already covers it.

If last round has multiple matches, it picks the last one; OK but slightly arbitrary.

Pair history / rematches

Adapter doesn’t consult PairingHistory to avoid rematches (maybe the engine handles it; if not, could add in Pass B—but only if spec requires it for Random mode).

Random determinism

Uses default Random() → non-deterministic between runs. Acceptable for Random mode.

Performance / IDs

NextMatchId(all) uses Max each time; OK for small lists.

In the loop: MatchId = NextMatchId(all) + nextRound.Count — works correctly, IDs increment.

Usability

GenerateNextRoundFair() doesn’t return the built matches (it only logs). Controller must refresh via GetMatches(). That’s fine.

Logging noise

Adapter is verbose (OK with logging toggle).

Targets for Pass B (safe cleanup)

Remove ViewModels using.

Add a tiny RoundLabels helper or reuse a shared constants class to standardize labels (“Round N” vs short codes).

Optionally return the generated matches from GenerateNextRoundFair() for easier controller updates (only if call sites allow).

(Optional) If project spec requires rematch avoidance, integrate PairingHistory check when pairing.

Verdict: Solid adapter; minor hygiene only.

-----------------------------------------------


Pass A — review only. Reading RaceEngines/RoundRobinEngineAdapter.cs.

RoundRobinEngineAdapter.cs — Review Notes (no code changes)

Usings

RCDragManagerProd.ViewModels and RCDragManagerProd.Repositories are not used → drop in cleanup.

Role

Wraps RoundRobinEngine to the IRaceEngine contract, plus extra helpers (standings, top N, completion).

Good

Clean passthroughs (LoadDrivers, GenerateBracket→GenerateMatches, Reset).

GetMatches() maps to EngineMatch and logs each line; BYE shows as (BYE) (null preserved).

Extra utilities: GetStandings(), IsTournamentComplete(), GetTopRankedDrivers(int).

Notes / Risks

EngineMatch mapping omits FromMatch1/FromMatch2 (fine for RR; there’s no pathing).

Logging is verbose; OK behind settings.

GetStandings() does FirstOrDefault lookup on each group; small N so fine, but we can preindex for clarity.

Round labels come from _engine.GetRoundLabels(); ensure consistency with UI filters (will verify later).

Targets for Pass B (safe cleanup)

Remove unused usings.

Minor perf/readability: cache _engine.GetAllDrivers() into a dictionary for GetStandings().

Add small summary log in GenerateBracket(): round labels list.

Keep BYE as null (don’t fabricate “BYE” drivers).

Verdict: Solid. Only hygiene.

------------------------------------------------------------

ass A — review only. Reading RaceEngines/RaceEngineFactory.cs.

RaceEngineFactory.cs — Review Notes (no code changes)

Usings

Unused: System.Collections.Generic, System.Linq, RCDragManagerProd.Domain, RCDragManagerProd.ViewModels.

Keep only: using System; and using RCDragManagerProd.Logging;.

Behavior

Guards blank raceType.

Normalizes to cleanType = ToLowerInvariant().Trim().

Maps synonyms → adapters:

Pro Ladder: "pro ladder", "nhra pro ladder"

Round Robin: "round robin"

Random: "random", "randomized", "random draw"

Logs requests, creations, and unknown types. Good.

Risks / edge cases

Stringly-typed race names: must match UI exactly. Safer long-term to centralize with an enum + parser (keep string inputs for UI, map to enum).

Curly quotes in the exception message: “{raceType}” → replace with straight quotes "{raceType}" in cleanup.

Missing common aliases: e.g., "pro", "nhra", "rr", "round-robin" (hyphen) — optional additions later.

Targets for Pass B (safe cleanup)

Remove unused usings.

Normalize exception quotes.

Optionally: add a static RaceTypeConstants (strings) or an enum RaceTypeKind + TryParse helper (non-breaking to callers; we can keep this factory signature).

Verdict: Simple and fine; just hygiene.

======================================================

Pass A — review only. Reading RandomMode/RandomMatch.cs.

RandomMatch.cs — Review Notes (no code changes)

Purpose

Simple DTO for randomized scheduling.

Fields

MatchId, Seed1/Seed2 (Driver refs), FromMatch1/FromMatch2 (rarely used in random mode), RoundLabel (free text like “Round 1”).

Good

Minimal, clear.

Driver refs kept (not IDs), which matches adapter usage.

Risks / cleanup targets (Pass B)

Consistency with EngineMatch: consider aligning names (Driver1/Driver2) or keep as-is and map cleanly—no behavior change.

If random mode doesn’t use FromMatch*, mark with comment or remove later (only if truly unused).

Add ToString() for debugging: M{MatchId} {RoundLabel}: {Seed1?.Name} vs {Seed2?.Name}.

Verdict: Fine as a DTO.
------------------------------------

Pass A — review only. Reading RandomMode/RandomMatchEngine.cs.

RandomMatchEngine.cs — Review notes (no code changes)

Usings

Drop: System.Text.RegularExpressions, RCDragManagerProd.RaceEngines (unused).

State/API

Holds bracketMatches, results (MatchResult), drivers.

LoadMatches, GetMatches, SetWinner, GetWinner/Loser/HasWinner, ResolveDrivers, IsTournamentComplete, RewindToMatch, LoadDrivers, GenerateBracket, Reset, GetRoundOrder.

Good

Downstream seeding patch in SetWinner fills Seed1/Seed2 where FromMatch* pointed to the decided match.

Bracket generator builds R1 pairs (pads with BYE via null) and wires successive rounds with FromMatch*. Final relabeled "F".

Problems / risks

Reference equality bug (loser detection).
GetLoserFromMatch compares Driver by reference: match.Seed1 == winner / match.Seed2 == winner. If winner is a different instance (same Id), loser resolves to null.
➜ Fix in cleanup: compare by Id.

BYE placeholders leak.
ResolveDrivers manufactures new Driver { Name = "BYE" }. This pollutes pairing history and CAN be saved as a “driver”.
➜ Keep null for BYEs; let UI render (BYE); optionally auto-advance.

No BYE auto-advance.
If a match is (driver vs null), HasWinner stays false until user sets it. Round progression stalls unless controller special-cases.
➜ In cleanup: auto-set winner when one side is null (once).

No guards.
SetWinner accepts null winner; LoadMatches accepts any list (no id duplicates check).
➜ Add basic guards + logs.

Ordering assumption for completion.
IsTournamentComplete assumes last match in list is the final. Safe for the generated bracket, but fragile if matches injected out of order.
➜ Optional: pick by last round label or highest MatchId.

Logging missing.
None here. We want debug logs for: load/generate, set winner, rewind, auto-advance, and resolve fallbacks.

Minor

Duplicate null checks / dictionary lookups you can collapse with TryGet.

drivers only used by GenerateBracket; everything else ignores it.

Cleanup targets (Pass B)

Compare drivers by Id (not reference) wherever determining loser/equality.

Remove “BYE driver” fabrication; preserve nulls; add auto-advance when (d1 != null && d2 == null) or vice versa (set result once).

Add logging at key ops; keep behind AppSettings.EnableLogging.

Drop unused usings; tidy code style.

Optional: stronger IsTournamentComplete by label "F" or max MatchId.

--------------------------------------------------------

Pass A — review only. Reading RandomMode/RandomBracket.cs.

RandomBracket.cs — Review Notes (no code changes)

Role

Static helpers to generate randomized rounds + track BYEs across the event.

Good

Tracks BYEs with byeGiven to avoid repeats.

Avoids rematches using pairingHistory in next rounds.

Issues / risks

Bug: first-round BYE selection loses a driver.

if (byeGiven.Contains(byeDriver.Id)) {
    byeDriver = shuffled.First(d => !byeGiven.Contains(d.Id));
    shuffled[i] = byeDriver; // ← overwrites slot i; original driver at i is lost; byeDriver now appears twice
}


Needs an actual swap by index, not overwrite.

Round labels inconsistent.
First round uses "R1", later rounds use "Next". This complicates UI/ordering. We should emit "R2", "R3", … or a consistent scheme.

MatchId resets.
GenerateNextRound starts matchId = 1 every time. If these matches are appended to an existing schedule, IDs will collide.

Null/edge guards missing.

GenerateFirstRound assumes drivers not null.

GenerateNextRound assumes non-null lists; if remainingDrivers has 1 item (shouldn’t, but could), code path relies on BYE block.

Logger imported but unused.
Add debug logs for generated pairs, BYE assignment.

Duplicate/unused helper.
NormalizePair(Guid, Guid) is unused here.

Targets for Pass B (safe cleanup)

Fix BYE swap bug (use indices).

Standardize labels to "R1","R2",... or constants.

Accept a startingMatchId parameter (or compute from existing list) to avoid ID collisions.

Add null guards and logs.

Remove unused Guid helper.

---------------------------------------------------------------

Pass A — review only. Reading RandomMode/LosersBracketEngine.cs.

LosersBracketEngine.cs — Review Notes (no code changes)

Role

Runs a blind single-elimination losers bracket with BYEs padded to power-of-two; winner returned via raceCallback.

Good

Clean loop over rounds; BYE handling is correct.

Clear logging at each stage.

Power-of-two padding helper is simple and safe.

Risks / edge cases

raceCallback can be slow or throw; current code will bubble exceptions (fine). Consider guarding/logging around callback.

No cancellation token—OK for WinForms sync flow.

Uses Random() without seed (fine for this mode).

Targets for Pass B (safe cleanup)

Null/arg guards: throw ArgumentNullException if raceCallback is null.

Minor perf/readability: pre-size next = new List<Driver?>(pool.Count/2).

Optional: record match IDs or emit a simple RandomMatch list if you plan to display a losers bracket (out-of-scope now).

-----------------------------------------------

Pass A — review only. Reading RandomMode/LosersBracketBuilder.cs.

LosersBracketBuilder.cs — Review Notes (no code changes)

Role

Builds a full losers-bracket schedule (IDs start at startMatchId, default 1000). Pads to power-of-two with BYEs and wires subsequent rounds via FromMatch*.

Good

Starts IDs at an offset to avoid collisions with main bracket.

Skips BYE-vs-BYE slots in R1 (prevents junk matches).

Logs each step; clear labels (Losers Bracket Rn / Final).

Issues / risks

history parameter is never used.
Comment says “avoid rematches,” but pairing doesn’t consult history.

Magic 0s for FromMatch*.
You’re setting FromMatch1/2 to 0 instead of null. Everywhere else, “unset” uses null. Zero is a silent magic number.

Unused helper.
Norm(int,int) not used.

Usings:
RCDragManagerProd.RaceEngines is imported but not used.

ID collisions across multiple builds.
You rely on the caller to pass a new startMatchId each time. If they forget, IDs will collide.

Targets for Pass B (safe cleanup)

Remove unused using, remove Norm.

Use null (not 0) for FromMatch* in all created matches.

Either (a) consume history to avoid rematches in R1 when possible, or (b) remove the parameter until implemented.

Add a tiny helper to return int lastId so the controller can compute the next startMatchId for subsequent builds.

Verdict: Structure is fine; just hygiene + either implement or drop the history feature.

--------------------------------------------

Pass A — review only. Reading RoundRobinMode/RoundRobinMatch.cs.

RoundRobinMatch.cs — Review Notes (no code changes)

Wrong namespace (major): File sits under RoundRobinMode/ but the class is in the global namespace. It should be namespace RCDragManagerProd.RoundRobinMode { ... }. This mismatch will break references like RCDragManagerProd.RoundRobinMode.RoundRobinMatch (you already used that type elsewhere).

Self-using: using RCDragManagerProd.RoundRobinMode; at top while not being in that namespace is odd and unnecessary if you place the class in that namespace.

DTO shape is fine: MatchId, Driver1, Driver2, RoundLabel.

BYE semantics: If odd participants, allow Driver2 == null (keep null; don’t fabricate “BYE” drivers).

Label consistency: Use the same short codes as other engines ("R1","R2",…) or central constants.

Targets for Pass B

Wrap in correct namespace.

Remove the self-using.

Optionally seal the DTO and add a simple ToString() for debugging.

---------------------------------------------------------

Pass A — review only. Reading RoundRobinMode/RoundRobinEngine.cs.

RoundRobinEngine.cs — Review Notes (no code changes)

What it does

Circle-method generator for 3 rounds. Pads odd roster with null (BYE). Stores matches as tuples; tracks results in MatchResult.

Good

Correct circle rotation (fix index 0, rotate tail).

BYE handled by inserting a (real,null) match.

Clean API: LoadDrivers, GenerateMatches, GetMatches, Set/Has/Get Winner, Reset, GetRoundLabels, standings helpers.

Issues / risks

Reference-equality bug in standings.
GetStandings() does .GroupBy(d => d) → that groups by object reference, not driver identity. If the same driver appears as different instances (likely), counts are wrong.
➜ Group by d.Id and map back.

Leaky BYE placeholders.
ResolveDrivers(LadderMatch) fabricates new Driver { Name = "BYE" }. We should keep null for BYE; adapters/UI can display (BYE). Also: why does a RR engine take a ProLadder.LadderMatch? Looks like a leftover helper—should be removed.

Weird dependency alias.
using LadderMatch = RCDragManagerProd.Domain.ProLadder.LadderMatch; only for that stray method. Drop both in cleanup.

Ordering of round labels.
GetRoundLabels() uses Distinct() without ordering. UI may get R2,R1,R3.
➜ Order by round index.

No logging inside engine.
Adapter logs a lot, but minimal engine logs (OK). We’ll keep it lightweight or add a couple of debug lines (generation summary).

Null guards.
LoadDrivers assumes non-null; small guard would help.

Tuple storage
Using tuple list; fine, but mapping back creates verbosity. Not a blocker.

Targets for Pass B (safe cleanup)

Fix standings: group by Id, not by reference.

Remove ResolveDrivers(LadderMatch) and the alias; RR shouldn’t depend on ProLadder types.

Keep BYEs as null—no fabricated “BYE” drivers anywhere.

Sort GetRoundLabels() (e.g., by parse of R{n}).

Add tiny guards and optional debug logs.

------------------------------------------------------

Pass A — review only. Reading RoundRobinMode/RoundRobinRanker.cs.

RoundRobinRanker.cs — Review Notes (no code changes)

Usings

using RCDragManagerProd.DicEx; looks wrong. Manifest shows Utils/DictEx.cs. Likely RCDragManagerProd.Utils. Also: you aren’t using any symbols from it here.

What it does

Computes per-driver points from matches + MatchResult.

Points schedule via PointsForRound("R1"/"R2"/"R3").

Tracks basic head-to-head _h2h to break ties.

Builds DriverRankResult list and sorts by Points → Wins → H2H → OpponentStrength → DriverId.

Issues / risks

OpponentStrength never computed (but used in sort).
You set OpponentStrength = 0 and then sort by it. It has no effect.
→ We need to actually compute SoS (sum of opponents’ final points faced; ignore BYEs).
Also, you only store DefeatedIds; you need all opponents faced, not just defeated, to compute SoS.

Grouping by reference elsewhere (engine)
This ranker is fine (uses IDs), but note RoundRobinEngine’s GetStandings() grouped by object (we’ll fix there in Pass B).

Missing guards / dictionary keys
If a match references a driver not present in drivers, stats[winnerId] will KeyNotFound. You partly guard with idToName, but stats build depends on drivers list only.
→ Either ensure drivers is authoritative, or lazily TryAdd into stats when encountering a new id.

Unknown round labels → 0 points
Fine, and you log it. Make sure engine only emits R1–R3.

BYE handling
Awards BYE points to the winner only (good). But for SoS, BYEs should not contribute opponents.

Minor

_h2h stores winner id only; good for 1 match between two drivers. If they meet twice, last result overwrites prior. Acceptable for short series.

Targets for Pass B (safe cleanup)

Remove/ fix using RCDragManagerProd.DicEx; (likely unused / wrong namespace).

Track all opponents faced per driver (e.g., add HashSet<int> Opponents to Aggregate and add both sides for every resolved match).

After computing points for all matches, compute OpponentStrength:

foreach (var row in table)
    row.OpponentStrength = row.DefeatedIds
       .Concat(alsoLostToIds[row.DriverId])
       .Distinct()
       .Sum(oppId => tableById[oppId].Points);


(We’ll implement cleanly in Pass B.)

Keep logs minimal.

Verdict: Core is close; main gap is OpponentStrength calculation and a stray using.

Pass A — review only. Reading RoundRobinScorecardLogger.cs.

RoundRobinScorecardLogger.cs — Review Notes (no code changes)

What it does

Two outputs:

Log(...) writes detailed scorecards + standings to the Logger.

BuildScorecard(...) returns a popup-friendly string with a composite score (Pts + tiny weights for Wins/H2H/SoS).

Good

Does not fabricate “BYE” drivers; treats null as BYE.

Points schedule matches Ranker (R1/R2/R3).

SoS computed as sum of opponents’ final totals actually faced (BYE excluded).

Clear per-match lines and final order.

Issues / cleanup targets (Pass B)

Unused using System.Data; — drop.

Round ordering uses lexicographic OrderBy(x => x); fine for R1–R3 but brittle if R10 appears. We’ll swap to numeric order helper.

Duplicate points logic vs RoundRobinRanker.PointsForRound. Let’s centralize to a single helper (non-breaking).

Logging is very verbose; keep it but behind AppSettings.EnableLogging (it already is via Logger).

Minor null-safety: a couple of dictionary lookups assume keys exist; we’ll use TryGetValue consistently.

Verdict: Solid diagnostics tool; only hygiene.

---------------------------------------------

Pass A — review only. Reading Controllers/RaceController.cs.

RaceController.cs — Review Notes (no code changes)

What it does well

Clean separation: controller talks to IRaceEngine adapters only; UI via events.

Strong logging throughout (good for debugging).

Round-reveal tracking via _revealedRounds is clear.

RR → Buyback → Finals flow is orchestrated with snapshots so RR stays visible after engine swap. Nice.

Bugs / risky spots

Losers-champion resolver will often bail out early.
RunLosersBracketChampion() checks _session.Matches.OfType<RandomMatch>() for MatchId >= 1000 and returns if none, but GenerateLosersBracket()/StartLosersBracket() never save lbMatches to _session.Matches. Result: it logs “No LB matches found” and returns null even though _selectedDrivers is set.
Fix in Pass B: either (a) persist lbMatches to _session.Matches, or (b) remove that check and run bracket solely from _selectedDrivers.

Duplicate entry points for Losers Bracket.
GenerateLosersBracket(...) and StartLosersBracket() do almost the same thing. This invites drift.
Fix: keep one public method, have the other delegate to it or delete it.

Unused fields.
_losersMatches is declared but never used. _drivers is only used by ResolveDriverIdByName (which itself is unused).
Fix: remove both unless you plan to use them.

Old builder left in the file.
BuildPairingRows() is superseded by BuildCurrentBracketRows(). It’s not referenced.
Fix: delete BuildPairingRows() (and keep ToPairingRow for NextMatch).

Race type string scatter + case sensitivity.
You compare and assign "Round Robin", "Losers Bracket", "Finals" in multiple places with varying casing.
Fix: centralize canonical constants (e.g., RaceTypes.RoundRobin, etc.) and use ordinal-ignore-case comparisons.

Round ordering from engines may be unsorted.
You rely on adapter order; if an engine ever returns ["R2","R1","R3"], reveal logic and list rendering could look odd.
Fix: sort labels numerically when consuming: R1<R2<…<SF<F.

Winner selection safety relies on “BYE” name strings.
You protect SubmitWinner against BYE via winner == null || winner.Name == "BYE". Engines inconsistently return null vs “BYE” placeholders.
Fix: standardize: engines should return null for BYE; controller should treat null as BYE. Remove string checks.

SaveSession result IDs for missing values.
You save WinnerDriverId = -1 / LoserDriverId = -1 when absent. Make sure repo layer tolerates -1. If not, prefer 0 or omit the row.
Fix: align with persistence expectations.

RR score logging duplication.
LogRoundRobinScoreboard() exists but you already use RoundRobinScorecardLogger.Log(...) and popup.
Fix: consider removing or calling it from a single place to avoid divergent outputs.

MessageBox in controller.
There are a couple of MessageBox.Show calls (buyback skipped notice). That couples controller to UI.
Fix: surface these via events so the Form decides if/how to show UI.

Hygiene / minor

Remove unused usings if any (looks fine here).

_results is just _matchResult; you can reference the field directly to reduce indirection.

ResolveDriverIdByName is dead; delete it.

PickWinnerCallback is dead; delete it (or wire it for your LB callback if needed).

In GetRoundOrder().First() access during initial reveal, guard empty (defensive).

Consistency checks (engines + controller)

Controller assumes EngineMatch.Driver1/Driver2 can be null (BYE). Ensure all adapters follow that (we’ll enforce during engine cleanup).

Round labels used: "R1" … "R3", "SF", "F", plus "Losers Bracket Rn". BuildCurrentBracketRows() handles mixed labels correctly; good.

--------------------------------------------------------

PairingRow.cs — quick review → tiny safe cleanup.

No functional issues. I’ll just harden it for nulls and make debugging easier.

Replace the file with this:

// PairingRow.cs
// Lightweight DTO for the bracket ListView

using System.Diagnostics;

namespace RCDragManagerProd.ViewModels
{
    [DebuggerDisplay("{MatchNumber} {RoundLabel} | {Driver1} vs {Driver2} (Hdr={IsHeader})")]
    public sealed class PairingRow
    {
        /// <summary>Engine MatchId; –1 when this row is a round header.</summary>
        public int MatchId { get; set; }

        public string RoundLabel { get; set; } = string.Empty;

        public string Driver1 { get; set; } = string.Empty;

        public string Driver2 { get; set; } = string.Empty;

        /// <summary>True if this row is a header (“Round 1”, “SF”, …).</summary>
        public bool IsHeader { get; set; }

        public string MatchNumber { get; set; } = string.Empty;
    }
}

---------------------------------------------
WinnerRow.cs — review only (no changes yet)

Looks fine and matches how RaceController fills it (MatchId, RoundLabel, Winner, Loser).

Pass B TODOs (when we clean)

Add null-safe defaults (= string.Empty) to avoid nulls in UI binding.

Add [DebuggerDisplay] for easier inspection.

(Optional) Keep property order consistent with display: RoundLabel, MatchId, Winner, Loser.

---------------------------------------

Looks good. Defaults avoid nulls; ToString is safe. No action required.

Pass B TODOs (later)

(Optional) [DebuggerDisplay("{EventDate:yyyy-MM-dd} — {EventName} ({ClassType} / {RaceType})")] for easier debugging.

------------------------------------------

MatchLookupHelper.cs — review only

Findings:

Hard-wired to ProLadder. If the active race is Round Robin or Random, this will return the wrong match or mislead callers.

Uses session.DriverEntries.Count (older path). Most code now uses session.Drivers. Risk of mismatch.

No null checks on session / DriverEntries.

using System.IO; is unused.

Return type ProLadder.LadderMatch ties callers to ladder internals; the app standard is EngineMatch.

Pass B cleanup TODOs

Either delete this helper if unused (preferred), or

Refactor to accept the current engine’s matches and return an EngineMatch:

public static EngineMatch FindMatch(IRaceEngine engine, int matchId) → engine.GetMatches().FirstOrDefault(m => m.MatchId == matchId);

If retained temporarily, add null guards and switch to session.Drivers.Count.

-----------------------------------------

AssetPath.cs — review only

Findings:

Namespace typo: RCDragManagerProd.Helpers.Helpers (double Helpers). This will break using RCDragManagerProd.Helpers; references.

Unused usings: System.Linq, RCDragManagerProd.Domain.

Always logs on every call; fine for debug, but can be chatty.

Pass B cleanup TODOs

Fix namespace to RCDragManagerProd.Helpers.

Remove unused usings.

(Optional) Add existence check and warn if asset missing.

(Optional) Cache BaseDirectory to avoid repeated lookups.

------------------------------------------------------

DatabaseInitializer.cs — review only

Looks solid: opens connection, enables FKs, creates 3 tables with sensible defaults, adds indexes.

Noted for Pass B (cleanup/system hardening):

Guard input: throw if connectionString is null/blank.

Add PRAGMAs for smoother desktop use: journal_mode=WAL; and synchronous=NORMAL; after open.

Indexes: consider IX_Drivers_Name (prefix search) if you do lookups by name; and IX_RaceSessions_ClassType if you filter by class often.

Minor: keep schema alignment—Cars uses CarID (your domain alias covers it), so no change needed.

-------------------------------------------------------------

DriverRepository.cs — review
What’s good

Safe parameterized SQL everywhere ✅

Helpful connection-string normalizer for file paths ✅

Child rows (Cars) inserted with the same connection during AddDriver ✅

JSON parser for historical wins is defensive and tolerant of older shapes ✅

Issues / opportunities

FKs aren’t guaranteed on every connection.
PRAGMA foreign_keys=ON is per-connection in SQLite. You set it in DatabaseInitializer, but each new Open() should enable it, otherwise cascades and FK checks may not apply.

N+1 queries in GetAllDrivers.
You open a new connection per driver to fetch cars. This is fine for tiny datasets but will crawl with dozens of drivers. Fetch all cars in one query and group in-memory.

No transactions for multi-step writes.
AddDriver (insert + cars) and UpdateDriver (update, delete cars, reinsert) should be atomic. Wrap in a transaction.

Delete flow duplicates cascade.
You delete from Cars explicitly, then from Drivers. With FKs + ON DELETE CASCADE, deleting the driver alone is enough (and safer). Keep your explicit delete if you prefer clarity, but it’s redundant once (1) is fixed.

GetCarsByDriverId doesn’t set DriverId.
You populate CarID, CarName, ClassType, DefaultDialIn. Consider also setting DriverId to keep objects complete.

Minor polish

Consider explicit column lists (SELECT Id, Name, …) instead of SELECT * for schema resilience.

Add an index on Drivers(Name) if you do name lookups.

Consider busy_timeout and WAL for better desktop UX (fewer “database is locked” hiccups).

Suggested Pass B changes (drop-in)
1) Harden Open() for every connection
private SQLiteConnection Open()
{
    var cn = new SQLiteConnection(_connStr);
    cn.Open();

    // Per-connection pragmas
    using (var cmd = new SQLiteCommand("PRAGMA foreign_keys = ON;", cn))
        cmd.ExecuteNonQuery();
    using (var cmd = new SQLiteCommand("PRAGMA journal_mode = WAL;", cn))
        cmd.ExecuteNonQuery();
    using (var cmd = new SQLiteCommand("PRAGMA synchronous = NORMAL;", cn))
        cmd.ExecuteNonQuery();
    using (var cmd = new SQLiteCommand("PRAGMA busy_timeout = 3000;", cn))
        cmd.ExecuteNonQuery();

    return cn;
}

2) Batch-load cars in GetAllDrivers (remove N+1)
public List<Driver> GetAllDrivers()
{
    Logger.Log("[DB][DriverRepo] GetAllDrivers()");
    var drivers = new List<Driver>();
    using var cn = Open();

    // Load drivers
    using (var cmd = new SQLiteCommand(
        "SELECT Id, Name, QualTime, Notes, TotalWins, TotalLosses, EventsEntered, EventsWon, State FROM Drivers", cn))
    using (var r = cmd.ExecuteReader())
    {
        while (r.Read())
        {
            drivers.Add(new Driver
            {
                Id            = r.GetInt32(0),
                Name          = r.GetString(1),
                QualTime      = r.IsDBNull(2) ? (double?)null : r.GetDouble(2),
                Notes         = r.IsDBNull(3) ? "" : r.GetString(3),
                TotalWins     = r.GetInt32(4),
                TotalLosses   = r.GetInt32(5),
                EventsEntered = r.GetInt32(6),
                EventsWon     = r.GetInt32(7),
                State         = r.IsDBNull(8) ? "" : r.GetString(8),
            });
        }
    }

    if (drivers.Count == 0) return drivers;

    // Load all cars and group by DriverId
    var carsByDriver = new Dictionary<int, List<Car>>();
    using (var cmd = new SQLiteCommand(
        "SELECT CarID, DriverId, CarName, ClassType, DefaultDialIn FROM Cars", cn))
    using (var r = cmd.ExecuteReader())
    {
        while (r.Read())
        {
            var car = new Car
            {
                CarID        = r.GetInt32(0),
                DriverId     = r.GetInt32(1),                 // <— set it
                CarName      = r.IsDBNull(2) ? "" : r.GetString(2),
                ClassType    = r.IsDBNull(3) ? "" : r.GetString(3),
                DefaultDialIn= r.IsDBNull(4) ? (double?)null : r.GetDouble(4)
            };
            if (!carsByDriver.TryGetValue(car.DriverId, out var list))
                carsByDriver[car.DriverId] = list = new List<Car>();
            list.Add(car);
        }
    }

    // Attach cars
    foreach (var d in drivers)
        d.Cars = carsByDriver.TryGetValue(d.Id, out var list) ? list : new List<Car>();

    Logger.Log($"[DB][DriverRepo] GetAllDrivers → {drivers.Count} rows");
    return drivers;
}

3) Make write ops atomic

AddDriver

public void AddDriver(Driver driver)
{
    if (driver == null) throw new ArgumentNullException(nameof(driver));
    Logger.Log($"[DB][DriverRepo] AddDriver(Name='{driver.Name}')");

    using var cn = Open();
    using var tx = cn.BeginTransaction();
    try
    {
        const string sql = @"
INSERT INTO Drivers (Name, QualTime, Notes, TotalWins, TotalLosses, EventsEntered, EventsWon, State)
VALUES (@Name, @QualTime, @Notes, @TotalWins, @TotalLosses, @EventsEntered, @EventsWon, @State);
SELECT last_insert_rowid();";

        using (var cmd = new SQLiteCommand(sql, cn, tx))
        {
            cmd.Parameters.AddWithValue("@Name", driver.Name);
            cmd.Parameters.AddWithValue("@QualTime", (object)driver.QualTime ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Notes", driver.Notes ?? string.Empty);
            cmd.Parameters.AddWithValue("@TotalWins", driver.TotalWins);
            cmd.Parameters.AddWithValue("@TotalLosses", driver.TotalLosses);
            cmd.Parameters.AddWithValue("@EventsEntered", driver.EventsEntered);
            cmd.Parameters.AddWithValue("@EventsWon", driver.EventsWon);
            cmd.Parameters.AddWithValue("@State", driver.State ?? string.Empty);
            driver.Id = Convert.ToInt32(cmd.ExecuteScalar());
        }

        if (driver.Cars != null)
            foreach (var car in driver.Cars)
                AddCar(car, driver.Id, cn, tx);

        tx.Commit();
        Logger.Log($"[DB][DriverRepo] AddDriver → new Id={driver.Id}");
    }
    catch
    {
        try { tx.Rollback(); } catch { }
        throw;
    }
}


UpdateDriver

public void UpdateDriver(Driver driver)
{
    if (driver == null) throw new ArgumentNullException(nameof(driver));
    Logger.Log($"[DB][DriverRepo] UpdateDriver(Id={driver.Id}, Name='{driver.Name}')");

    using var cn = Open();
    using var tx = cn.BeginTransaction();
    try
    {
        const string sql = @"
UPDATE Drivers SET 
    Name=@Name, QualTime=@QualTime, Notes=@Notes, TotalWins=@TotalWins, 
    TotalLosses=@TotalLosses, EventsEntered=@EventsEntered, EventsWon=@EventsWon, State=@State
WHERE Id=@Id";
        using (var cmd = new SQLiteCommand(sql, cn, tx))
        {
            cmd.Parameters.AddWithValue("@Name", driver.Name);
            cmd.Parameters.AddWithValue("@QualTime", (object)driver.QualTime ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Notes", driver.Notes ?? string.Empty);
            cmd.Parameters.AddWithValue("@TotalWins", driver.TotalWins);
            cmd.Parameters.AddWithValue("@TotalLosses", driver.TotalLosses);
            cmd.Parameters.AddWithValue("@EventsEntered", driver.EventsEntered);
            cmd.Parameters.AddWithValue("@EventsWon", driver.EventsWon);
            cmd.Parameters.AddWithValue("@State", driver.State ?? string.Empty);
            cmd.Parameters.AddWithValue("@Id", driver.Id);
            cmd.ExecuteNonQuery();
        }

        // Replace cars
        using (var del = new SQLiteCommand("DELETE FROM Cars WHERE DriverId=@DriverId", cn, tx))
        {
            del.Parameters.AddWithValue("@DriverId", driver.Id);
            del.ExecuteNonQuery();
        }
        if (driver.Cars != null)
            foreach (var car in driver.Cars)
                AddCar(car, driver.Id, cn, tx);

        tx.Commit();
        Logger.Log("[DB][DriverRepo] UpdateDriver → OK");
    }
    catch
    {
        try { tx.Rollback(); } catch { }
        throw;
    }
}


DeleteDriver (let cascade do the work)

public void DeleteDriver(int id)
{
    Logger.Log($"[DB][DriverRepo] DeleteDriver(Id={id})");
    using var cn = Open();
    using var tx = cn.BeginTransaction();
    using (var cmd = new SQLiteCommand("DELETE FROM Drivers WHERE Id=@Id", cn, tx))
    {
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.ExecuteNonQuery();
    }
    tx.Commit();
    Logger.Log("[DB][DriverRepo] DeleteDriver → OK");
}


AddCar overload that participates in a transaction

private void AddCar(Car car, int driverId, SQLiteConnection cn, SQLiteTransaction tx)
{
    const string sql = @"
INSERT INTO Cars (DriverId, CarName, ClassType, DefaultDialIn)
VALUES (@DriverId, @CarName, @ClassType, @DefaultDialIn);";

    using var cmd = new SQLiteCommand(sql, cn, tx);
    cmd.Parameters.AddWithValue("@DriverId", driverId);
    cmd.Parameters.AddWithValue("@CarName", car.CarName ?? string.Empty);
    cmd.Parameters.AddWithValue("@ClassType", car.ClassType ?? string.Empty);
    cmd.Parameters.AddWithValue("@DefaultDialIn", (object)car.DefaultDialIn ?? DBNull.Value);
    cmd.ExecuteNonQuery();
}


(Keep your existing AddCar(int, Car) overload as a convenience wrapper that opens a connection and calls this.)

Quick test checklist (after Pass B changes)

Create, read, update, delete a driver with multiple cars; verify atomicity (force an exception during car insert and ensure no partial writes).

Verify that deleting a driver removes cars without explicit car delete.

Run GetAllDrivers() with many drivers and confirm only two queries happen (drivers + cars).

Force a competing writer and confirm busy_timeout surfaces fewer “database is locked” errors.

----------------------------------------------------------

RaceSessionRepository.cs — review & fixes

What’s good

Simple, parameterized insert + last_insert_rowid() 👍

CamelCase JSON snapshotting keeps the session portable 👍

GetAllSessions returns a lean RaceSessionSummary 👍

Issues / improvements

SQLite PRAGMAs per-connection. Enable FKs/WAL/timeout in every Open() (not just at DB init).

Date parsing robustness. You save "yyyy-MM-dd HH:mm:ss"; parse with ParseExact to avoid locale issues.

DRY: NormalizeConnString duplicates DriverRepo logic → consider a shared helper (optional).

Small safety: wrap SaveSession insert in a transaction (not required, but nice).

Drop-in edits (minimal)
using System.Globalization;
// ...

private SQLiteConnection Open()
{
    var cn = new SQLiteConnection(_connStr);
    cn.Open();

    // Harden each connection
    using (var cmd = new SQLiteCommand("PRAGMA foreign_keys = ON;", cn)) cmd.ExecuteNonQuery();
    using (var cmd = new SQLiteCommand("PRAGMA journal_mode = WAL;", cn)) cmd.ExecuteNonQuery();
    using (var cmd = new SQLiteCommand("PRAGMA synchronous = NORMAL;", cn)) cmd.ExecuteNonQuery();
    using (var cmd = new SQLiteCommand("PRAGMA busy_timeout = 3000;", cn)) cmd.ExecuteNonQuery();

    return cn;
}

public List<RaceSessionSummary> GetAllSessions()
{
    Logger.Log("[DB][SessionRepo] GetAllSessions()");
    var list = new List<RaceSessionSummary>();

    const string sql = @"
SELECT Id, EventName, EventDate, ClassType, RaceType
FROM RaceSessions
ORDER BY EventDate DESC"; // string format y-M-d HH:mm:ss sorts lexicographically already

    using (var cn = Open())
    using (var cmd = new SQLiteCommand(sql, cn))
    using (var rd = cmd.ExecuteReader())
    {
        while (rd.Read())
        {
            DateTime dt = DateTime.MinValue;
            if (!rd.IsDBNull(2))
            {
                var s = rd.GetString(2);
                DateTime.TryParseExact(s, "yyyy-MM-dd HH:mm:ss",
                    CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out dt);
            }

            list.Add(new RaceSessionSummary
            {
                Id        = rd.GetInt32(0),
                EventName = rd.IsDBNull(1) ? "" : rd.GetString(1),
                EventDate = dt,
                ClassType = rd.IsDBNull(3) ? "" : rd.GetString(3),
                RaceType  = rd.IsDBNull(4) ? "" : rd.GetString(4)
            });
        }
    }

    Logger.Log($"[DB][SessionRepo] GetAllSessions → {list.Count} rows");
    return list;
}


(Optional) If you want SaveSession fully atomic:

public int SaveSession(object session)
{
    // ... build eventName, classType, raceType, eventDate, json
    const string sql = @"
INSERT INTO RaceSessions (EventName, EventDate, ClassType, RaceType, SessionData)
VALUES (@EventName, @EventDate, @ClassType, @RaceType, @SessionData);
SELECT last_insert_rowid();";

    using var cn = Open();
    using var tx = cn.BeginTransaction();
    try
    {
        int newId;
        using (var cmd = new SQLiteCommand(sql, cn, tx))
        {
            cmd.Parameters.AddWithValue("@EventName", eventName ?? "");
            cmd.Parameters.AddWithValue("@EventDate", eventDate.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("@ClassType", classType ?? "");
            cmd.Parameters.AddWithValue("@RaceType", raceType ?? "");
            cmd.Parameters.AddWithValue("@SessionData", json ?? "{}");
            newId = Convert.ToInt32(cmd.ExecuteScalar());
        }
        tx.Commit();
        TrySetIntProp(session, "Id", newId);
        Logger.Log($"[DB][SessionRepo] SaveSession → Id={newId}");
        return newId;
    }
    catch { try { tx.Rollback(); } catch { } throw; }
}

CarRepository.cs — review & fixes

Biggest problem: this repository uses System.Data.SqlClient (SQL Server) and assumes columns Id/DriverId, but the rest of your codebase (and schema in DatabaseInitializer) uses SQLite with CarID as the PK:

CREATE TABLE Cars (
  CarID INTEGER PRIMARY KEY AUTOINCREMENT,
  DriverId INTEGER NOT NULL,
  CarName TEXT NOT NULL,
  ClassType TEXT NOT NULL,
  DefaultDialIn REAL,
  FOREIGN KEY (DriverId) REFERENCES Drivers(Id) ON DELETE CASCADE
);


Also, DriverRepository already implements AddCar and GetCarsByDriverId against SQLite. Keeping a second, divergent CarRepository will cause drift and bugs.

Recommendation (pick one):

Best: delete CarRepository and use the car methods already in DriverRepository.

If you want to keep it: switch it to SQLite, use CarID (not Id), and don’t rely on car.DriverId (your Car model in the rest of the code doesn’t carry DriverId). Pass the driverId into the methods.

Drop-in replacement (SQLite, aligned columns, thin & correct)
using System.Collections.Generic;
using System.Data.SQLite;
using System;
using RCDragManagerProd.Domain;
using RCDragManagerProd.Logging;

namespace RCDragManagerProd.Repositories
{
    public sealed class CarRepository
    {
        private readonly string _connStr;
        public CarRepository(string connectionOrPath)
        {
            if (string.IsNullOrWhiteSpace(connectionOrPath)) throw new ArgumentNullException(nameof(connectionOrPath));
            _connStr = DriverRepository /* or a shared helper */.GetType() != null
                ? NormalizeConnString(connectionOrPath) // copy same logic you use elsewhere
                : connectionOrPath;
        }

        private static string NormalizeConnString(string input)
        {
            if (input.IndexOf('=') >= 0 &&
                input.IndexOf("Data Source", StringComparison.OrdinalIgnoreCase) >= 0)
                return input;
            var path = input;
            if (!System.IO.Path.IsPathRooted(path))
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var folder = System.IO.Path.Combine(appData, "RC_Drag_Manager");
                System.IO.Directory.CreateDirectory(folder);
                path = System.IO.Path.Combine(folder, path);
            }
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path) ?? ".");
            return $"Data Source={path};Version=3;";
        }

        private SQLiteConnection Open()
        {
            var cn = new SQLiteConnection(_connStr);
            cn.Open();
            using (var cmd = new SQLiteCommand("PRAGMA foreign_keys=ON;", cn)) cmd.ExecuteNonQuery();
            using (var cmd = new SQLiteCommand("PRAGMA journal_mode=WAL;", cn)) cmd.ExecuteNonQuery();
            using (var cmd = new SQLiteCommand("PRAGMA synchronous=NORMAL;", cn)) cmd.ExecuteNonQuery();
            using (var cmd = new SQLiteCommand("PRAGMA busy_timeout=3000;", cn)) cmd.ExecuteNonQuery();
            return cn;
        }

        public void AddCar(int driverId, Car car)
        {
            if (car == null) throw new ArgumentNullException(nameof(car));
            using var cn = Open();
            const string sql = @"INSERT INTO Cars (DriverId, CarName, ClassType, DefaultDialIn)
                                 VALUES (@DriverId, @CarName, @ClassType, @DefaultDialIn);";
            using var cmd = new SQLiteCommand(sql, cn);
            cmd.Parameters.AddWithValue("@DriverId", driverId);
            cmd.Parameters.AddWithValue("@CarName", car.CarName ?? "");
            cmd.Parameters.AddWithValue("@ClassType", car.ClassType ?? "");
            cmd.Parameters.AddWithValue("@DefaultDialIn", (object)car.DefaultDialIn ?? DBNull.Value);
            cmd.ExecuteNonQuery();
            Logger.Log($"[DB][CarRepo] AddCar → DriverId={driverId}, Car='{car.CarName}'");
        }

        public List<Car> GetCarsByDriver(int driverId)
        {
            var cars = new List<Car>();
            using var cn = Open();
            const string sql = @"SELECT CarID, DriverId, CarName, ClassType, DefaultDialIn
                                 FROM Cars WHERE DriverId=@DriverId";
            using var cmd = new SQLiteCommand(sql, cn);
            cmd.Parameters.AddWithValue("@DriverId", driverId);
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                cars.Add(new Car
                {
                    CarID        = rd.GetInt32(0),
                    // DriverId field is optional in your Car model; omit if you don’t have it there
                    CarName      = rd.IsDBNull(2) ? "" : rd.GetString(2),
                    ClassType    = rd.IsDBNull(3) ? "" : rd.GetString(3),
                    DefaultDialIn= rd.IsDBNull(4) ? (double?)null : rd.GetDouble(4)
                });
            }
            Logger.Log($"[DB


RaceSessionRepository (SQLite) — review

Connection safety: consider enabling foreign_keys, WAL, and a small busy timeout on every open connection (PRAGMAs are per-connection).

Dates: you save as "yyyy-MM-dd HH:mm:ss" but parse with a general DateTime.Parse; using a strict parse avoids locale surprises.

SaveSession works as-is; a short transaction would make it more resilient (optional).

NormalizeConnString logic is duplicated across repos—worth extracting to a shared helper later.

CarRepository — review

Mismatch: it uses SQL Server (System.Data.SqlClient) while the rest of your app (and schema) is SQLite.

Column names don’t line up with your schema (CarID vs Id, etc.).

Functionality overlaps with the car methods already present in DriverRepository → risk of drift. Either remove this repository or convert it to SQLite and align columns/types with your existing schema.

If you want, I’ll keep doing concise, no-code review bullets for the next files you paste.

-------------------------------------------------------------------------------

got it—this is the big one. overall the Form is in good shape and lines up with the controller/events you built. here are the must-fix items and a few tight patches you can drop in right now:

must-fix / correctness

Quick-session driver IDs can collide with DB IDs
You treat “Id > 0” as “exists in DB”, but btnAddDriver_Click gives new quick drivers positive IDs. That can accidentally bump the wrong DB row.
Fix: give ad-hoc drivers negative IDs.

// Form1 fields
private int _tempDriverId = -1;

// in btnAddDriver_Click (new driver path)
var newDriver = new Driver
{
    Id = _tempDriverId--,   // negative ids for ad-hoc drivers
    Name = name,
    QualTime = qualTime
};


Winners list ordering misses “Round N” labels (Random engine)
GetGlobalRoundOrder handles R1/R2/... and LB, but not "Round 1". Those rounds default to 800 and sort oddly.
Fix: parse textual “Round N”.

private int GetGlobalRoundOrder(string roundLabel)
{
    if (string.IsNullOrWhiteSpace(roundLabel)) return 999;
    if (roundLabel.Equals("F", StringComparison.OrdinalIgnoreCase)) return 1000;
    if (roundLabel.Equals("SF", StringComparison.OrdinalIgnoreCase)) return 990;

    if (roundLabel.StartsWith("Round ", StringComparison.OrdinalIgnoreCase))
    {
        if (int.TryParse(roundLabel.Substring(6).Trim(), out var n)) return 100 + n;
    }

    if (roundLabel.StartsWith("Losers Bracket", StringComparison.OrdinalIgnoreCase))
    {
        var label = roundLabel.Trim();
        if (label.EndsWith("Final", StringComparison.OrdinalIgnoreCase)) return 299;
        var parts = label.Split(' ');
        if (parts.Length >= 3)
        {
            var last = parts[^1];
            if ((last.Length >= 2) && (last[0] == 'R' || last[0] == 'r') &&
                int.TryParse(last.Substring(1), out var n)) return 200 + n;
        }
        return 290;
    }

    if ((roundLabel.Length >= 2) && (roundLabel[0] == 'R' || roundLabel[0] == 'r') &&
        int.TryParse(roundLabel.Substring(1), out var r)) return 100 + r;

    if (roundLabel.StartsWith("Semi", StringComparison.OrdinalIgnoreCase)) return 990;
    if (roundLabel.StartsWith("Final", StringComparison.OrdinalIgnoreCase)) return 1000;

    return 800;
}


Duplicate Winners handler
You already handle _controller.WinnersUpdated with an inline lambda (and it includes proper header grouping and global ordering). The extra OnWinnersUpdated method is dead code.
Fix: delete the unused method to avoid confusion.

Database not initialized anywhere
Repos will happily connect, but the tables may not exist on a fresh machine.
Fix: call your initializer once at app start (preferably in Program.Main), e.g.:

// Program.cs (before showing Form1)
DatabaseInitializer.InitializeDatabase("Data Source=race_data.db;Version=3;");


Hook CanPickWinnerChanged (polish + safety)
You mostly gate the buttons via OnNextMatchReady, but the controller also raises CanPickWinnerChanged. Tie it in so a late disable (e.g., event complete) always wins.

_controller.CanPickWinnerChanged += enabled =>
{
    // respect BYE guard too
    btnWinner1.Enabled = enabled && !IsByeName(btnWinner1.Text);
    btnWinner2.Enabled = enabled && !IsByeName(btnWinner2.Text);
};

good improvements (optional but nice)

Save & Close: recompute EventsWon from sessions (or remove incremental bump).
You already increment in TournamentCompleted. If you prefer a single source of truth, recompute on save and overwrite:

// inside the try in btnSaveAndClose_Click
var dRepo = new DriverRepository("race_data.db");
foreach (var d in drivers.Where(x => x.Id > 0))
{
    var db = dRepo.GetDriverById(d.Id);
    if (db == null) continue;
    db.EventsWon = dRepo.ComputeEventsWonFromSavedSessions(d.Id);
    dRepo.UpdateDriver(db);
}


(If you keep this, consider removing the final’s extra +1 in HandleWinnerClick/TournamentCompleted to avoid double-counting before this overwrite.)

Tag the pairing rows with MatchId (future QoL):
If you ever want double-click to jump to a match, set item.Tag = row.MatchId in RedrawFullBracket.

UI: only messagebox once on Buybacks/Finals
You already guard finals with finalsPopupShown; the buyback popup is fine, just note it will show every time the event toggles to enabled.

Null-safe combo restore:
When restoring cmbRaceType, ensure the item exists (set SelectedIndex after finding index).


