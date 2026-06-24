# Claude Worker Routine

Use this routine to implement exactly one GitHub issue.

## Inputs

Required:

- Repository: `stewmac570/RC-Drag-Manager`
- GitHub issue number

Optional:

- Target branch
- Extra user instructions

## Process

1. Read `AGENTS.md`.
2. Read the GitHub issue body, labels, comments, and linked issues.
3. Check whether the issue is blocked by another issue.
4. Inspect the relevant code before changing anything.
5. Create a branch named `codex/issue-<number>-short-title`.
6. Implement only the issue scope.
7. Add or update tests when behavior changes.
8. Run the required validation command.
9. Create or update a pull request.
10. Stop and report the PR, tests, risks, and follow-ups.

## Required Validation

For normal app changes, run:

```powershell
dotnet test src\RCDragManagerProd.Tests\RCDragManagerProd.Tests.csproj -m:1 --logger "console;verbosity=minimal"
```

If the issue only changes docs or GitHub metadata, explain why tests were not run.

If tests fail:

- Identify whether the failure is new or pre-existing.
- Do not hide or ignore failures.
- Do not weaken tests without explaining the intended behavior.

## Scope Rules

- Work one issue only.
- Do not rewrite whole forms unless the issue explicitly requires it.
- Do not perform broad cleanup while implementing a narrow issue.
- Do not change race rules unless the issue explicitly requires it.
- Do not change persistence behavior unless the issue explicitly requires it.
- Do not touch installer/release logic unless the issue is release related.
- Do not start WPF migration unless the issue is WPF related.

## Architecture Rules

- Prefer UI-independent services/view models for new workflow logic.
- Keep WinForms as the production UI unless the issue says otherwise.
- Preserve existing race-day behavior while extracting logic.
- Add service-level tests for extracted behavior.
- Avoid adding logic directly into form event handlers.

## Output

Report:

- Issue worked
- Branch name
- Pull request URL
- Summary of changes
- Tests run and exact result
- Known risks
- Follow-up issues created or recommended
