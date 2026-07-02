# 2026-07-02 — Report store consumer map checkpoint

Status: checkpoint after PR #50.

## Context

PR #50 added a repository-local scan for report-store consumers and connected it to the `.NET Build` workflow.

The scan is intentionally based on the checked-out source tree instead of external GitHub code search.

The current scanned symbols are:

```text
MooringShapeStore
MooringIterativeSolverStore
```

## Current limitation

The CI run can print the consumer map to the job log, but long workflow logs can be truncated by connector output.

That means the workflow log is useful as proof that the scan runs, but it is not a stable architecture document.

## Boundary decision

Do not use external GitHub code search as the source of truth for report-store consumers.

Do not manually guess store declarations or consumers from partial log output.

The source of truth for the next audit step is:

```text
pwsh -NoProfile -ExecutionPolicy Bypass -File tools/scan-report-store-consumers.ps1
```

from a full repository checkout.

## Next safe step

Create a dedicated repository-local map artifact or committed audit document from the local scan output.

The first version should stay documentation-only or tooling-only.

It must not change:

```text
- solver physics
- numerical formulas
- generated Markdown report text
- PDF output
- 2D output
- UI behavior
- store behavior
```

## Safety gate

Every follow-up PR must keep `.NET Build` green.

Any PR that changes store ownership, consumers, or rendering paths must explicitly say so in the PR description.

By default, this phase remains architecture stabilization only.
