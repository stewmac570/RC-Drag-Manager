# Handover — RC Drag Manager: "forms are pure UI" extraction epic

_Last updated: 2026-06-11_

## The goal
Reduce **every** WinForms form to *pure UI* (code-behind = control wiring + rendering +
dialog show/close only; all workflow/validation/persistence delegated to services in
`RCDragManagerProd.AppServices`). The app must look & behave **identically** (no visible
change). Then **tag that commit as the clean "ready for new UI" release** so the new UI can
be built on UI-independent services. This is the #283 epic.

## Working agreement
- Assistant acts as senior dev; the owner merges PRs manually and smoke-tests the UI. Pick
  the next screen yourself — don't ask.
- **One screen per branch/PR, batching several slices per branch** (owner asked for "more
  per branch" — not one tiny PR per slice).
- Per branch: create `<Screen>Service` → rewire the form → MSBuild → full `dotnet test`
  (green) → stage **only the assistant's own files** (the working tree carries ~25 unrelated
  dirty `Docs/`/`.claude/` files — keep them out of every commit) → file-based commit ending
  with `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>` → push → **one** PR with a
  manual smoke-test checklist → **wait for the owner's "merged"** (never auto-merge).

## Key technical notes
- **Build:** MSBuild via PowerShell (not Git Bash):
  `& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" "src\RCDragManagerProd\RCDragManagerProd.sln" /t:Build /p:Configuration=Debug /v:minimal /nologo`
- **Test:** `dotnet test src\RCDragManagerProd.Tests\RCDragManagerProd.Tests.csproj --nologo -v minimal --blame-hang-timeout 90s`
  — run **plain in the background**, never with a `2>&1 | Select-String` pipe (hangs PowerShell 5.1).
- Namespace is **`AppServices`** (NOT `Application` — collides with `System.Windows.Forms.Application`).
- New **production** `.cs` files need a `<Compile Include>` in the non-SDK
  `RCDragManagerProd.csproj`; the test project is SDK-style (no entry needed).
- MSTest throw assertion is `Assert.ThrowsExactly<T>` (not `ThrowsException`).
  `TemporarySqliteDb` is a per-file private nested test helper (copy it; it's not shared).
- Forms can't be GUI-verified by the assistant — that's why every PR ships a smoke checklist
  for the owner to run before merging.

## Done (merged)
Race console (`Form1`) fully extracted into `RaceConsoleService` + `RaceConsoleViewModel`/
builder (PRs #321/#323/#324/#325/#326): build/start, advance, standings, buyback,
winner-submit (BYE + lane-swap mapping), edit-result, and save/close via an
`IRaceSessionStore` seam. Plus bug fix #322 (QMDRA finals was clearing the match-up grid).

## In flight
**PR #327** `refactor/drivermanager-service-286` — `DriverManagerService` extracted from
`DriverManagerForm` (#286). **Awaiting owner merge.** `main` is at the #326 merge.

## Remaining batches to the tag (~5–7 PRs, rough order)
1. **#288 LoadSessionForm** — load/delete sessions & events (do this next after #327 merges).
2. **#287 MultiClassConfigDialog** (~677 lines, biggest) + **MultiClassSetupForm**.
3. **#289 MultiClassRaceForm** — multi-class coordination, stats, completion.
4. **Form1 residual** — setup-roster add/edit driver + qual-time validation in
   `Form1.Events.cs`, and `OnTournamentCompleted` stats.
5. Audit smaller forms/dialogs (Settings, DriverStats, Add*/Edit* dialogs) — extract any
   validation; leave pure input dialogs as-is.
6. Final grep audit (no repository / validation / business logic left in any form) →
   **tag the release**.

## First step in a new session
Check whether PR #327 is merged (`gh pr view 327 --json state`).
- **If merged:** sync `main`, delete the merged branch, then start **#288 LoadSessionForm**
  (read `src/RCDragManagerProd/UI/Forms/Session/LoadSessionForm.cs`, extract load/delete of
  sessions & events into a `LoadSessionService`).
- **If not merged yet:** wait for the owner.

Test baseline after #286: **215 passed / 11 skipped / 0 failed**. Known noise: CS0618 at
`RaceController.EngineCalls.cs:46`; MSTEST0037 analyzer suggestions; 11 multi-class skips.
