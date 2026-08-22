# Control Mark — F5-C exact-main release consumer gates

Date: 2026-08-23
Issue: #555
Parent: #522
Starting main: `19bd87cc18c895b53825b4b27cbcaa78b783dc46`

## Problem fixed

Before F5-C the standalone workflows:

```text
Selected Shape Consumer Scan
Report Store Consumer Scan
```

ran on pull requests and manual dispatch only. They therefore could not provide independent `push` evidence for the exact `main` commit used to build the v1.0.0 Release Candidate.

## Change

Both workflows retain their existing `pull_request` and `workflow_dispatch` triggers and additionally run on:

```text
push -> main
```

Their job bodies, scan scripts, artifact outputs and permissions remain unchanged.

## Release gate

Before creating/updating `release-candidate/v1.0.0`, require on the same exact `main` SHA:

```text
.NET Build: success
Selected Shape Consumer Scan: success
Report Store Consumer Scan: success
BuoyCalc Windows Build: success
```

Only after that evidence is green may the RC trigger ref be pointed to that same main SHA.

## Invariants

F5-C changes workflow triggers only. No production code, solver/physics, selected geometry, F1/F2/F3/F4 authority, canonical engineering baseline, release package contents, persistence, PDF/2D behavior or 3D is changed.
