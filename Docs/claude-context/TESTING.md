# RC Drag Manager — Testing Policy

This is the testing guardrail for the project (issue #292). Every UI/UX refactor
should ship with runnable tests so behaviour is verified while coding — not by
asking the owner to manually click through the app. Manual app testing is the
**final smoke check**, never the primary verification method.

---

## Standard Commands

The solution targets .NET Framework 4.8 (WinForms). Build with MSBuild or Visual
Studio — **not** `dotnet build`. Tests run under `dotnet test`.

**Build (Debug):**

```
MSBuild.exe src/RCDragManagerProd/RCDragManagerProd.sln /t:Build /p:Configuration=Debug
```

**Run the full test suite:**

```
dotnet test src/RCDragManagerProd.Tests/RCDragManagerProd.Tests.csproj
```

The test project uses in-memory / temp SQLite — no external setup required. On
Windows, run MSBuild from PowerShell (not Git Bash, which mangles `/t:` `/p:`
switches).

### Baseline

A clean run is **all green** with a small, known set of skipped tests (the
multi-class `ValidateClassName` / `ValidateCanStart` gates). The only expected
build noise is the `CS0618` obsolete-API warning at
`RaceController.EngineCalls.cs` and nullable warnings in the test project. If the
baseline is not green, fix that **before** starting a refactor (see issue #302).

---

## What Every UI/UX Refactor Should Cover

When extracting or changing a workflow, add or update **service/controller-level**
tests (no WinForms controls, no dialogs to close) covering:

- **Command state changes** — which actions/buttons are enabled per race phase.
- **Validation failures** — bad input is rejected with the expected outcome.
- **Persistence outcomes** — saves write through the repositories; reloads restore.
- **Resume / load behaviour** — an interrupted event resumes where it left off.
- **No-window execution** — the test runs head­less under `dotnet test`.

Keep UI smoke testing (visually confirming layout/appearance on the running app)
**separate** from service-workflow testing.

---

## Test Foundation (headless helpers)

Reusable seams live in `src/RCDragManagerProd.Tests/Helpers/` (issue #290). Prefer
them over re-deriving inline boilerplate:

| Helper | Use |
|--------|-----|
| `TestDriverFactory` | Driver packs (`CreateProLadderPack`, `CreateProLadderByePack`, `CreateRoundRobinPack(n)`). |
| `TestSessionFactory` | `RaceSession` / `MultiClassEvent` builders (`ProLadder`, `RoundRobin`, `WithDriverEntries`, `MultiClassEvent`). |
| `NoOpStandingsDialogService` | Suppress the standings dialog in headless runs. |
| `RecordingStandingsDialogService` | Record `Show(...)` calls to assert the standings seam was (or wasn't) invoked. |

`RaceController` accepts an `IStandingsDialogService` so dialog display can be
doubled in tests. As more UI workflow is extracted into application services
(issues #283/#284), introduce matching interfaces/doubles for prompts,
confirmations, input dialogs, navigation, and repositories, and add them here.

---

## PR Expectations

- Every UI/UX or refactor PR **lists the test command run** and its result in the
  test plan.
- New behaviour comes with a failing-then-passing test where practical.
- Do not change passing tests to make a refactor look clean — add new ones.
