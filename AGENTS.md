# AGENTS.md — RC Drag Manager

## Working agreement

These instructions define how Codex works with Stewart McMillan in this repository. Read this file first, then read the repository-level `CLAUDE.md`. Before writing code, read the project documents that `CLAUDE.md` marks as always required and any task-specific documents it identifies.

Stewart is the solo developer. Focus reviews and changes on real correctness, data-loss, performance, architecture, and security risks—not style policing.

## Project context

RC Drag Manager is a Windows desktop application for running NHRA-style RC drag racing tournaments. It uses C#, .NET Framework 4.8, WPF, legacy WinForms, and SQLite. The primary UI is `RCDragManagerProd.WPF`; WinForms is legacy and should remain untouched unless Stewart explicitly asks for work there.

The repository's `CLAUDE.md` is the source of truth for current architecture, domain conventions, protected areas, build commands, testing, and documentation routing. If this summary conflicts with it, follow `CLAUDE.md`.

## Git workflow — mandatory

Unless Stewart explicitly directs otherwise:

1. Never commit directly to `main`, `master`, `trunk`, or another default branch.
2. Create a fresh branch from the default branch for each logical change:
   - `feature/<short-name>` for functionality
   - `fix/<short-name>` for bug fixes
   - `docs/<short-name>` for documentation-only changes
   - `chore/<short-name>` for cleanup, dependencies, builds, or tooling
3. Keep one logical change per branch and PR. If scope expands materially, stop and ask Stewart before including unrelated work.
4. When asked to complete the Git workflow, push the branch and open a PR with a clear summary and test/verification plan.
5. Never merge a PR without explicit approval such as “merge it” or “ship it.” Positive feedback on a diff is not merge approval. Stewart performs or explicitly authorizes merges.
6. Never push directly or force-push to a protected/default branch.
7. If a commit is accidentally made on the default branch, stop, do not push, tell Stewart, and offer to move it to a branch and restore the local default branch to `origin/<default>`.

Always require explicit approval before merging a PR, pushing to a protected branch, force-pushing, deleting a branch/tag/release, publishing a release or installer, or running destructive Git operations such as `reset --hard`, `clean -f`, `branch -D`, `checkout --`, or `restore --`.

## Claims and verification — mandatory

1. Do not state a factual claim unless it was checked in the current turn. If it was not checked, say so explicitly.
2. Verify numbers and dates before presenting them as facts. Do not invent or estimate counts, durations, or timespans.
3. When Stewart corrects a claim about hardware, what he can see, or what he has already tried, treat his observation as authoritative input and check the relevant source immediately. Do not make him repeat it.
4. Record Stewart's established real-world or hardware findings in the relevant repository document in the same turn, including who established the finding and the date.
5. Do not try to enforce required behaviour with documentation alone. Use a test, hook, linter, or other executable check when recurrence must be prevented.
6. When correcting a stale claim, search the whole repository and update every occurrence in code, scripts, tools, documentation, and user-facing text in the same turn.
7. Clearly distinguish what has been proven in code/tests or on hardware from what has only been reasoned through. State what remains unknown.

## Implementation rules

- Follow all architecture rules and conventions in `CLAUDE.md`; they are non-negotiable.
- New UI work belongs in the WPF project.
- Keep race logic out of views. Preserve the View/Form → Service/Controller → Engine boundaries documented by the project.
- Keep database access in repositories.
- Do not introduce automatic race or round advancement.
- Use themed WPF resources and dialogs rather than hardcoded colours or `MessageBox`, subject to the startup exception documented in `CLAUDE.md`.
- Use `RoundLabels` constants and `ByePolicy.IsBye`; do not duplicate those rules.
- Use `System.Text.Json`, not Newtonsoft.Json.
- Do not modify protected files or areas listed under “What Not to Touch” in `CLAUDE.md` unless the task explicitly requires it.

## Build and verification

- This is .NET Framework 4.8. Do not use `dotnet build`.
- Build the repo-root `RCDragManagerProd.sln` with Visual Studio 2022 or MSBuild.
- Run the test project through Visual Studio Test Explorer or an appropriate compatible test runner.
- All tests must pass before committing.
- Report exactly what was run, whether it passed, and any verification that was not possible. Never describe work as built, tested, or working without evidence from the current turn.

## Review priorities

### 1. Logic and correctness

- Flag calculation errors in timing, speed, or performance-derived values.
- Check boundary conditions: lap/run counters, elapsed time, and index bounds.
- Flag off-by-one errors in run sequences or data arrays.
- Verify state transitions such as run in progress → complete → saved.

### 2. Data integrity

- Flag paths where run/log data could be silently lost or overwritten.
- Require file writes to be atomic where practical, or at minimum fail loudly.
- Flag missing null checks before writing telemetry or log entries.
- Ensure timestamps and run identifiers are applied before persistence.

### 3. Performance

- Flag UI-thread blocking from data loads, file I/O, or expensive calculations.
- Flag unnecessary re-reading of data that could be cached.
- Watch for tight loops over large telemetry datasets without early exits or batching.

### 4. Code structure and naming

- Flag ambiguous names in calculation or timing logic where clarity affects safety.
- Flag methods with mixed responsibilities, especially in data-processing paths.
- Suggest extraction when a method exceeds roughly 50 lines and mixes concerns.

### 5. Security

- Flag file paths constructed from unsanitized user input.
- Flag machine-specific hardcoded paths.
- Flag external data loaded without validation.

## What not to flag in reviews

- Minor formatting or whitespace.
- Preference-based naming when the project uses an abbreviation consistently.
- Missing XML documentation on private methods.
- Warnings in third-party or generated code.

## Review style

- Be direct and specific. Point to the line and explain the concrete risk.
- Suggest an obvious fix inline when one exists.
- Group related issues instead of repeating the same comment.
- Do not summarize what the code does; report only defects and meaningful risks.
