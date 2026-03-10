# Codex Change And Test Loop

## 1. Purpose
- This file defines the standard Codex workflow for making and validating changes safely in RC Drag Manager.
- It is the operating process for scoped edits, verification, and merge recommendations.

## 2. Workflow Goals
- Reduce regressions in race-critical behavior.
- Keep changes scoped to the requested task.
- Ensure review is reproducible from docs, diff, and verification notes.
- Ensure build, scenario, and log-marker verification happen before merge recommendation.
- Support future automated harness use by keeping verification steps explicit and structured.

## 3. Standard Codex Change Loop
1. Review relevant docs and code.
2. Identify impacted feature area(s) and risk level.
3. Create a feature branch for the requested work.
4. Make scoped change(s) only.
5. Build the solution.
6. Run relevant tests or verification steps for impacted flow.
7. Review logs and required markers where applicable.
8. Review git diff for scope and correctness.
9. Commit with a clear message.
10. Push branch to origin.
11. Provide completion summary, risk notes, and merge recommendation.

### Practical step notes for this repo
- Step 1 (review): start with
  - `Docs/qa/QA_TEST_STRATEGY.md`
  - `Docs/qa/QA_TEST_MATRIX.md`
  - `Docs/qa/QA_TEST_DATA.md`
  - `Docs/qa/QA_LOGGING_MARKERS.md`
  - then impacted source files in `src/RCDragManagerProd/`.
- Step 6 (verification): if no automated tests exist for the change area, use targeted manual scenario verification defined by QA matrix/data docs.
- Step 7 (logs): validate relevant markers in `%APPDATA%\RC_Drag_Manager\app.log` when flow-level behavior is changed.

### Canonical local commands (current harness baseline)
- Build command (preferred local baseline validation):
  - `dotnet build RCDragManagerProd.sln -c Debug`
- Full-suite automated test command (headless local baseline):
  - `dotnet test src/RCDragManagerProd.Tests/RCDragManagerProd.Tests.csproj -m:1 --logger "console;verbosity=minimal"`
- `MSBuild.exe RCDragManagerProd.sln /t:Build /p:Configuration=Debug /p:Platform="Any CPU"` may work in some developer environments, but should not be treated as the only assumed local build command.
- `dotnet build` is the safer default guidance for Codex/local shell use unless a branch or task explicitly confirms a different requirement.
- `-m:1` is currently recommended for local reliability/headless stability.
- These commands reflect the current harness baseline and may be updated if harness/tooling evolves.

## 4. Required Inputs Before A Change
- Relevant QA docs for strategy, matrix scenario IDs, test data packs, and logging markers.
- Impacted source files and neighboring flow files (especially controller partials and engine adapters).
- Existing logging markers currently used by that flow.
- Known assumptions / `Needs confirmation` items from QA docs.
- Mode impact scope:
  - single mode (Pro Ladder, Random, Round Robin)
  - or multi-mode/shared flow (`IRaceEngine`, controller round/persistence/UI mapping paths).

## 5. Verification Requirements
Codex should not recommend merge until these are addressed:
- Build success for affected solution/project.
- Targeted scenario review completed for impacted behavior.
- Applicable QA matrix items considered and referenced.
- Applicable log markers checked for changed transitions.
- No unrelated file edits included in final diff.
- Known risks and uncertainties explicitly called out.

### Minimum verification by change type
- Engine or controller flow changes:
  - build + targeted mode scenario(s) + marker checks.
  - if shared interface/flow changed, review all three race modes.
- Persistence/save-load changes:
  - build + save/load checkpoint scenario + save/load markers.
- UI winner/round controls:
  - build + manual click-path verification + winner/advance markers.
- Docs-only changes:
  - diff scope review only (no behavior claims beyond source-backed facts).

## 6. Merge Recommendation Rules
- `Merge now`:
  - requested scope complete,
  - verification performed for impacted area,
  - no unrelated edits,
  - no unresolved high-risk issue.
- `Keep open for follow-up`:
  - core task done, but additional low/medium-risk follow-up work is intentionally deferred (for example extra QA docs or broader scenario coverage).
- `Do not merge yet`:
  - build/verification failed,
  - high-risk behavior remains unverified,
  - or diff contains unintended/unrelated changes.

## 7. Scope Control Rules
- No unrelated refactors.
- No opportunistic cleanup unless explicitly requested.
- One feature/fix per branch where practical.
- Preserve useful existing logging markers.
- Do not change behavior outside requested scope.
- If broader changes are discovered as necessary, document the dependency and request/plan follow-up instead of expanding silently.

## 8. Future Harness Integration
- Future-state (not fully implemented yet): this loop can map directly to automated harness stages.
- Planned mapping:
  - Input stage: select QA matrix IDs + QA data pack IDs.
  - Execute stage: run build plus scripted scenario checks.
  - Observe stage: parse required markers from app log.
  - Report stage: produce pass/fail summary with diff + risk notes.
- For harness readiness, keep future Codex outputs structured with:
  - impacted feature area,
  - scenarios checked,
  - markers verified,
  - assumptions/needs-confirmation list,
  - merge recommendation reason.

## Needs confirmation
- If local/CI environments diverge in behavior, confirm whether command flags should vary by environment while keeping these local defaults.
- Automated harness scripts are future-state; current repo process remains build + targeted manual/log verification.
