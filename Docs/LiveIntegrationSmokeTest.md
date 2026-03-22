# Live Integration Smoke Test

## Scope
This document covers the current live update path from `RaceController` to `https://stewmacrc.com/api/update`, plus verification via `https://stewmacrc.com/api/live`.

## Current Wired Trigger Points (Exact)
1. `GenerateBracket` success path
- File: `src/RCDragManagerProd/Controllers/RaceController.RoundFlow.Core.cs:154`
- Call: `QueueLiveUpdate("GenerateBracket")`
- Runs after `_revealedRounds` initialized and `PushFullRefresh()` completes.

2. `AdvanceRound` success path
- File: `src/RCDragManagerProd/Controllers/RaceController.RoundFlow.Core.cs:199`
- Call: `QueueLiveUpdate("AdvanceRound")`
- Runs after round reveal + redraw + `PushNextMatch()` + `PushAdvanceState()`.

3. `SubmitWinner` success path
- File: `src/RCDragManagerProd/Controllers/RaceController.Results.cs:73`
- Call: `QueueLiveUpdate("SubmitWinner")`
- Runs after winner persisted and post-update controller flow.

## DTO Build + Send Path (Exact)
- Build method: `src/RCDragManagerProd/Controllers/RaceController.LiveUpdate.cs:15` (`BuildLiveRaceUpdateDto`)
- Queue method: `src/RCDragManagerProd/Controllers/RaceController.LiveUpdate.cs:68` (`QueueLiveUpdate`)
- HTTP send method: `src/RCDragManagerProd/Integration/LiveRaceUpdateClient.cs:18` (`SendAsync`)
- Timeout: `3s` at `LiveRaceUpdateClient.cs:15`
- Header key used: `X-API-KEY` at `LiveRaceUpdateClient.cs:49`
- URL source: `LiveUpdateUrl` at `LiveRaceUpdateClient.cs:30`
- API key source: `LiveUpdateApiKey` at `LiveRaceUpdateClient.cs:31`

## Expected Payload By Trigger
All triggers send full-state replacement payload with the same shape:
```json
{
  "eventName": "...",
  "eventDate": "yyyy-MM-dd",
  "currentRound": "...",
  "nextUp": "Driver A vs Driver B",
  "matches": [
    { "driver1": "...", "driver2": "..." }
  ]
}
```

### 1) After `GenerateBracket`
Expected:
- `eventName`: from `_session.EventName` (fallback `Quick Session`)
- `eventDate`: from `_session.EventDate` formatted `yyyy-MM-dd`
- `currentRound`: first revealed round
- `nextUp`: first unresolved match in visible round(s)
- `matches`: visible bracket rows (non-header), usually first round only at this point

### 2) After `SubmitWinner`
Expected:
- `currentRound`: same active round unless progression logic moved state
- `nextUp`: next unresolved match in revealed rounds, lane-adjusted names
- `matches`: full visible bracket snapshot after winner update

### 3) After `AdvanceRound`
Expected:
- `currentRound`: newly revealed round label
- `nextUp`: first unresolved match in now-visible rounds
- `matches`: expanded visible bracket list (newly revealed round included)

## Log Validation
Log config keys are in `src/RCDragManagerProd/App.config`:
- `LogFilePath` (`%APPDATA%\RC_Drag_Manager\app.log`)
- `LiveUpdateEnabled`
- `LiveUpdateUrl`
- `LiveUpdateApiKey`

Monitor logs in PowerShell:
```powershell
Get-Content "$env:APPDATA\RC_Drag_Manager\app.log" -Wait
```

For each trigger, expected marker sequence:
1. `[LIVE][BUILD] reason=<TriggerName> ...`
2. `[LIVE][SEND] POST https://stewmacrc.com/api/update`
3. terminal result:
- success: `[LIVE][OK] Status=200` (or another 2xx)
- failure: `[LIVE][FAIL] ...` or `[LIVE][FAIL] Status=<non-2xx>`

Skip conditions (no send):
- `[LIVE][SKIP] reason=... dto=null`
- `[LIVE][SKIP] reason=... invalidState ...`
- `[LIVE][SKIP] reason=... disabled=true`

## API Validation (`/api/live`)
Current endpoint behavior verified on **2026-03-21**:
- `GET https://stewmacrc.com/api/live` returned `200` with JSON shape:
```json
{"eventName":"","eventDate":"","currentRound":"","nextUp":"","matches":[]}
```

Check latest live state after each trigger:
```bash
curl -i https://stewmacrc.com/api/live
```

Validation criteria:
- response `200`
- fields present: `eventName`, `eventDate`, `currentRound`, `nextUp`, `matches`
- values reflect last trigger action (round/next-up/matches)

## Failure Cases To Test

### A) No internet
How:
- Disconnect network (or disable adapter), then execute one wired trigger.
Expected:
- race flow continues normally (no UI freeze/crash)
- `[LIVE][BUILD]` appears
- `[LIVE][FAIL]` appears (network exception text)
- once network returns, later triggers resume sends

### B) Bad API key
How:
- set `LiveUpdateApiKey` to invalid value in `App.config`, restart app, trigger update.
Expected:
- `[LIVE][SEND]` then `[LIVE][FAIL] Status=401`
- `/api/live` does not update from this bad-key request
Reference check (verified 2026-03-21): POST with bad key returned `401 {"error":"unauthorized"}`.

### C) Server timeout
How:
- set `LiveUpdateUrl` to a non-responsive endpoint (example: `http://10.255.255.1/api/update`), restart app, trigger update.
Expected:
- failure within ~3 seconds due to client timeout
- `[LIVE][FAIL]` timeout/connection error logged
- race flow unaffected

### D) Server returns non-200
How:
- set `LiveUpdateUrl` to known non-200 route (example: `https://stewmacrc.com/api/update-does-not-exist`), restart app, trigger update.
Expected:
- `[LIVE][SEND]` then `[LIVE][FAIL] Status=404` (or other non-2xx)
- race flow unaffected

## Race-Day Pass/Fail Checklist
Mark each item `PASS` / `FAIL`.

1. Trigger coverage
- `GenerateBracket` emits `[LIVE][BUILD] reason=GenerateBracket`
- `SubmitWinner` emits `[LIVE][BUILD] reason=SubmitWinner`
- `AdvanceRound` emits `[LIVE][BUILD] reason=AdvanceRound`

2. Payload quality
- `eventDate` always valid `yyyy-MM-dd`
- `currentRound` non-empty
- `matches` non-empty when send occurs
- `nextUp` matches actual next race pairing

3. Transport
- `[LIVE][SEND]` appears for each trigger
- success path shows `[LIVE][OK] Status=2xx`
- `/api/live` mirrors latest expected state

4. Resilience
- no-internet case logs `[LIVE][FAIL]` and race flow continues
- bad-key case logs `Status=401` and race flow continues
- timeout case fails quickly (~3s) and race flow continues
- non-200 case logs status and race flow continues

5. Operational controls
- setting `LiveUpdateEnabled=false` causes `[LIVE][SKIP] ... disabled=true`
- re-enable restores send behavior after restart

## Code Fixes During This Review
- No code changes made.
- No small live-path bug requiring immediate patch was identified in this review.
