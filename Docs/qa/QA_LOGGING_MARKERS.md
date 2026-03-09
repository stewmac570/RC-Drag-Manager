# QA Logging Markers

## 1. Purpose
- This file defines important logging markers and log-driven verification points for QA.
- It is the baseline for manual log validation now and future automated log checks later.

## 2. Logging Goals
- Verify critical race transitions.
- Verify winner submission flow.
- Verify BYE handling.
- Verify round reveal and advancement flow.
- Verify standings, finals, and event completion where applicable.
- Support change verification after Codex edits.
- Support future automated log checks.

## 3. Current Logging Sources
- Controller logging:
  - `src/RCDragManagerProd/Controllers/*` (`RaceController.*` partials).
  - Common markers include `[CTRL]`, `[WINNER]`, `[ROUND]`, `[DEBUG]`, `[SAVE]`, `[FINALS]`, `[LB]`, `[RESET]`, `[EngineCall]`.
- Engine logging:
  - `src/RCDragManagerProd/RaceEngines/*` adapters and `RaceEngineFactory`.
  - Round Robin engine logging in `src/RCDragManagerProd/RoundRobinMode/RoundRobinEngine.cs` and scorecard logger.
  - Common markers include `[ENGINE FACTORY]`, `[ENGINE-API]`, `[RR-ADAPTER]`, `[RND]`, `[RR]`, `[RR-SCORE]`, `[RR][QMDRA]`.
- UI logging:
  - `src/RCDragManagerProd/UI/Forms/Main/*`, `LandingPageForm.cs`, `LoadSessionForm.cs`, `SessionSetupForm.Events.cs`.
  - Common markers include `[UI]`, `[UI][WINNER]`, `[UI][CLICK]`, `[FORM1]`, `[CREATE]`, `[UI][LoadSession]`.
- Persistence/save-load logging:
  - `src/RCDragManagerProd/Controllers/RaceController.Persistence.cs` and `src/RCDragManagerProd/Repositories/RaceSessionRepository.cs`.
  - Common markers include `[SAVE]`, `[DB][SessionRepo]`, `[TX]`.
- Logging service and path:
  - Logger implementation: `src/RCDragManagerProd/Logging/Logger.cs`.
  - Path source: `src/RCDragManagerProd/Config/AppSettings.cs` (`%APPDATA%\RC_Drag_Manager\app.log`).
  - App startup/fatal markers emitted from `src/RCDragManagerProd/Program.cs`.

## 4. Marker Categories
- App/session startup
- Mode selection / engine creation
- Bracket generation
- Match render / next-up display
- Winner click / winner submit
- BYE auto handling / disabled choice handling
- Round completion / round reveal
- Round Robin standings / ranking
- QMDRA / buyback / losers bracket / finals flow
- Save/load / restore
- Event completion / reset

## 5. Recommended QA Verification Markers

| ID | Category | Marker / Event | Why It Matters | Verification Method | Notes |
|---|---|---|---|---|---|
| LOG-001 | App/session startup | `[APP] Startup` / `[APP] Database ready.` | Confirms runtime bootstrap, DB path, and logging state | Manual log check | Emitted from `Program.Main()` |
| LOG-002 | App/session startup | `[APP][UI-ERROR]`, `[APP][FATAL-DOMAIN]`, `[APP][ASYNC-ERROR]`, `[APP][FATAL]` | Captures unhandled exception paths | Manual log check + Automated log check later | Should remain rare in normal runs |
| LOG-003 | Mode selection / engine creation | `[ENGINE FACTORY] Requested race type` and `Creating ...Adapter` | Verifies selected race mode and adapter binding | Manual log check | Core sanity check after mode changes |
| LOG-004 | Bracket generation | `[CTRL][DEBUG] GenerateBracket inputs ...` and `[ENGINE] Bracket generated.` | Confirms generation request and completion | Manual log check | Includes RR variant and `RoundsToRun` context |
| LOG-005 | Bracket generation | `[ProLadderValidate] ...` markers | Validates Pro Ladder count/template gating | Manual log check | Required when testing driver count boundaries |
| LOG-006 | Match render / next-up display | `UI: Generate Next Round button enabled/disabled.` | Confirms round-gating state updates reach UI | Manual log check | Emitted from `Form1.OnCanAdvanceChanged` |
| LOG-007 | Winner click / winner submit | `[UI][CLICK] Winner1/2 clicked...` + `[UI][WINNER] Calling SubmitWinner...` + `[WINNER] M...` | Confirms end-to-end winner click mapping and accepted submission | Manual log check | Use with lane-swapped and normal cases |
| LOG-008 | Winner click / winner submit | `[WINNER] Reject ...` and `[UI][WINNER][ERROR] After SubmitWinner, winner is still null...` | Ensures rejection/invalid-path visibility | Manual log check | Validate rejection reasons are explicit |
| LOG-009 | BYE handling | `[WINNER][BYE] Auto-advance ...` and `[UI][WINNER] Mapping: Engine D1/D2 is BYE...` | Confirms BYE auto behavior and UI mapping logic | Manual log check + Code-path review | BYE marker presence is critical for odd-driver scenarios |
| LOG-010 | Round completion / reveal | `[SNAP] AdvanceRound-entry`, `[ROUND] Revealing round: ...`, `[DEBUG] PushAdvanceState...` | Verifies progression gating and reveal sequencing | Manual log check | Use alongside UI button state checks |
| LOG-011 | Round Robin standings / ranking | `[RR-SCORE] ...`, `[ROUND ROBIN] Final standings:`, `[RR-ADAPTER] Top ranked:` | Confirms RR scoring and ranking output path | Manual log check | Useful for standings regression checks |
| LOG-012 | QMDRA flow | `[RR][QMDRA] Check ...`, `[RR][QMDRA] COMPLETE ...`, `[FINALS][QMDRA] ...` | Confirms QMDRA completion and finals seeding path | Manual log check | High-priority markers for QMDRA regression |
| LOG-013 | Buyback / losers bracket flow | `GenerateLosersBracket wrapper called`, `Starting Losers Bracket`, `[LB] Engine swapped ...`, `Revealed: LB-R1` | Confirms LB entry and initial reveal flow | Manual log check | Some lines include emoji prefixes |
| LOG-014 | Finals flow | `Injecting Final-4 Pro Ladder bracket`, `[FINALS] Start request accepted`, `[FINALS] Finals gate lowered` | Verifies finals transition and gating lifecycle | Manual log check | Include no-buyback and LB-champion routes |
| LOG-015 | Save/load / restore | `[SAVE] ...` + `[DB][SessionRepo] SaveSession/LoadSession` + `[TX] BEGIN/COMMIT/ROLLBACK SaveSession` + `[UI][LoadSession] ...` | Confirms persistence and reload paths | Manual log check + Automated log check later | Required for mid-event save/load tests |
| LOG-016 | Event completion / reset | `[UI] TournamentCompleted ...`, `[UI] Event Complete acknowledged ...`, `[RESET] Controller cleared ...` | Confirms closure and reset lifecycle | Manual log check | Use in end-to-end completion scenario |
| LOG-017 | Engine call tracing | `[EngineCall] <Engine> <Method> matchId=... round=...` | Provides cross-cutting trace of controller->engine calls | Manual log check + Code-path review | Good anchor for future automation parsing |

## 6. Gaps / Needs confirmation
- Marker style is inconsistent (for example `[TAG]` markers mixed with plain text and emoji-prefixed messages).
- Some markers include non-ASCII symbols or punctuation variants that may complicate exact-string automation.
- There is overlap between controller and UI markers for similar transitions; canonical marker source per transition is not formally defined.
- Docs in `Docs/` include logging behaviors not fully aligned with current code (for example rotation/fallback narratives versus current `Logger` implementation).
- Needs confirmation: whether Release builds in QA environments always run with logging enabled (`AppSettings.EnableLogging` default is build-dependent).

## 7. Recommendations for Future Harness Support
- Normalize marker naming into a stable prefix convention (for example `[APP]`, `[CTRL]`, `[ENG]`, `[UI]`, `[DB]`).
- Ensure each critical transition logs once with clear success/failure states.
- Add explicit markers for transitions that currently rely on generic debug lines where needed.
- Keep existing useful markers stable; if changed, update QA docs and automation checks in the same change.
- For automation readiness, prefer markers with machine-friendly key/value pairs and avoid ambiguous punctuation.
- Add a small marker contract test list (expected/forbidden markers) once automated QA harness work starts.
