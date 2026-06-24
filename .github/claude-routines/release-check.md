# Claude Release Check Routine

Use this routine to check whether the app is ready for a release candidate.

## Inputs

Required:

- Repository: `stewmac570/RC-Drag-Manager`

Optional:

- Release branch
- Version number
- Include live server repo: `stewmac570/RCDragLiveServer`

## Process

1. Read `AGENTS.md`.
2. Review open issues marked for the next release.
3. Check open pull requests.
4. Check CI status.
5. Run or verify the app test command.
6. Verify installer/release package instructions.
7. Verify operator docs and release notes are current.
8. Report blockers and non-blocking polish.

## Required Validation

Run or verify:

```powershell
dotnet test src\RCDragManagerProd.Tests\RCDragManagerProd.Tests.csproj -m:1 --logger "console;verbosity=minimal"
```

If installer verification is available, run or verify the documented installer build command.

## Blockers

Treat these as release blockers unless explicitly waived:

- Failing test baseline.
- Open race-day correctness issues.
- Known save/resume data loss risk.
- Broken installer/update path.
- Missing operator docs for changed workflows.
- Live server publishing regression if live scoreboard is part of the release.

## Output

Use this shape:

```text
Release readiness:
ready / not ready

Blockers:
- ...

Non-blocking polish:
- ...

Tests:
- ...

Installer/release package:
- ...

Recommended next action:
- ...
```
