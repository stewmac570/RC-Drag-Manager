# Claude Routines

These files are reusable instructions for Claude/Codex-style workers.

The model is:

- `AGENTS.md` defines repo rules.
- GitHub issues define the work queue.
- Routine markdown files define how the worker behaves.
- Pull requests are the output.

## Routine Files

- `worker.md` - implement one GitHub issue.
- `reviewer.md` - review one pull request.
- `fixer.md` - address review comments or failing CI on one pull request.
- `reporter.md` - summarize repo status and recommend the next task.
- `release-check.md` - verify release readiness.

## Standard Usage

Give the routine an issue or pull request number and tell it which file to read.

Examples:

```text
Read .github/claude-routines/worker.md and work issue #302.
```

```text
Read .github/claude-routines/reviewer.md and review PR #123.
```

```text
Read .github/claude-routines/fixer.md and fix PR #123.
```

## Hard Rules

- One issue per branch.
- One pull request per issue.
- Do not auto-merge.
- Do not change unrelated files.
- Do not start WPF work unless the issue explicitly asks for WPF.
- Preserve race-day correctness over cosmetic changes.
- Run the required tests and report the exact result.
