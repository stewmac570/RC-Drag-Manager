# Claude Reporter Routine

Use this routine to summarize current project status and recommend the next task.

## Inputs

Required:

- Repository: `stewmac570/RC-Drag-Manager`

Optional:

- Include live server repo: `stewmac570/RCDragLiveServer`

## Process

1. Read open issues relevant to the next-release backlog.
2. Read open pull requests.
3. Check CI status where available.
4. Identify blocked issues and dependencies.
5. Recommend the next best issue to work.
6. Do not change code.

## Priority Order

Recommend work in this order:

1. Green test baseline.
2. CI and automation gates.
3. Save/resume/recovery safety.
4. UI-independent service/view-model extraction.
5. Button-free workflow tests.
6. Current WinForms UI/UX cleanup.
7. Live server polish.
8. Release/installer polish.
9. Operator docs.
10. WPF prototype after contracts/services exist.

## Output

Use this shape:

```text
Status:
- ...

Open PRs:
- ...

Blocked:
- ...

Next recommended issue:
- #123 - reason

Risks:
- ...
```

Keep the report short enough to use as a daily status update.
