# Claude Reviewer Routine

Use this routine to review exactly one pull request.

## Inputs

Required:

- Repository: `stewmac570/RC-Drag-Manager`
- Pull request number

## Process

1. Read `AGENTS.md`.
2. Read the pull request title, body, linked issue, comments, and changed files.
3. Compare the diff against the linked issue scope.
4. Review for race-day correctness, data safety, test coverage, and scope creep.
5. Check whether tests were run and whether CI passed.
6. Leave a review with findings or approval.

## Review Priorities

Follow `AGENTS.md` priorities:

1. Logic and correctness.
2. Data integrity.
3. Performance and UI thread blocking.
4. Code structure where it affects maintainability or future UI migration.
5. Security and unsafe file/path handling.

## What To Flag

- Race state transitions that can become wrong.
- Save/resume/close behavior that can lose or corrupt race data.
- UI changes that allow unsafe race-day actions.
- Missing tests for changed workflow logic.
- PRs that change unrelated files.
- WPF work started before UI-neutral contracts/services exist.
- CI or local test failures without explanation.

## What Not To Flag

- Minor formatting.
- Preference-only naming.
- Missing XML docs on private methods.
- Existing warnings unrelated to the PR.

## Output

Start with findings.

Use this shape:

```text
Findings:
- [P1/P2/P3] File and line: issue and risk.

Missing tests:
- ...

Risk:
low / medium / high

Decision:
approve / request changes / comment only
```

If there are no findings, say so clearly and mention any remaining test or release risk.
