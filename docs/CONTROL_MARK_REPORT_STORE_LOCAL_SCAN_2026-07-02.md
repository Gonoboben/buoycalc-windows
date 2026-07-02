# 2026-07-02 — Report store local scan

Status: done in branch `store-scan`.

## Purpose

Bring the report-store consumer audit under repository-local control.

External GitHub code search was not reliable enough for this audit. It returned no results for symbols that are known to exist in the repository.

Therefore the project now uses a local PowerShell scan against the checked-out source tree.

## Added tool

```text
tools/scan-report-store-consumers.ps1
```

The script scans all C# source files under the repository checkout, excluding `bin` and `obj`.

It checks these report-store symbols:

```text
MooringShapeStore
MooringIterativeSolverStore
```

For each symbol it prints:

```text
- physical declaration path
- total references
- write references
- read candidate references
- explicit read references
- every reference line with path and line number
```

## CI connection

The `.NET Build` workflow runs this script after the existing checks:

```text
check-reportbuilder-usage.ps1
check-technical-report-path.ps1
check-readmodel-boundary.ps1
scan-report-store-consumers.ps1
```

## Boundary decision

Do not rely on external GitHub code search for store-consumer architecture decisions.

Use repository-local scans from the checked-out source tree.

## Non-goals

This checkpoint does not change:

```text
- solver physics
- numerical formulas
- generated Markdown output
- PDF output
- 2D output
- UI behavior
- store behavior
```

## Next step

Use the CI output from `scan-report-store-consumers.ps1` to create the real consumer map.
