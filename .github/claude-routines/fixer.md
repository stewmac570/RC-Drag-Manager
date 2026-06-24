# Claude Fixer Routine

Use this routine to address review comments or failing CI on exactly one pull request.

## Inputs

Required:

- Repository: `stewmac570/RC-Drag-Manager`
- Pull request number

## Process

1. Read `AGENTS.md`.
2. Read the pull request, review comments, and CI failures.
3. Identify the smallest fix that addresses the feedback.
4. Change only files needed for the review or CI failure.
5. Run the relevant tests.
6. Push updates to the same pull request branch.
7. Reply with what was fixed and what remains.

## Rules

- Do not add new feature work.
- Do not broaden the PR scope.
- Do not rewrite code unrelated to the review/CI failure.
- Do not dismiss a failing test as unrelated without proof.
- Do not auto-merge.

## Required Validation

Run:

```powershell
dotnet test src\RCDragManagerProd.Tests\RCDragManagerProd.Tests.csproj -m:1 --logger "console;verbosity=minimal"
```

If the failure is CI-only, inspect CI logs and explain the environment difference.

## Output

Report:

- PR fixed
- Review comments addressed
- CI failures addressed
- Tests run and exact result
- Remaining risk or unresolved failures
