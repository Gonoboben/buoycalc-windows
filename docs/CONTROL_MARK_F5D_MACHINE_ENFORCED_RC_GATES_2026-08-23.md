# Control Mark — F5-D machine-enforced RC CI gates

Date: 2026-08-23
Issue: #557
Parent: #522
Starting exact main: `f3bd76b2e0c69b7734f9b47660d752d5d532059b`

## Purpose

Make the Windows Release Candidate workflow independently enforce the exact-main CI gate before packaging. External observation of CI is no longer sufficient to authorize the RC build.

## Existing source identity gate

The release workflow continues to require:

```text
HEAD == github.sha
HEAD == origin/main
```

This ensures the RC source is exactly the current `main` commit.

## Machine-enforced CI gate

Before `package-windows-rc.ps1` runs, the release workflow queries the GitHub Actions REST API for the exact `github.sha` and `event=push` and requires the latest matching run for each workflow to be:

```text
.NET Build                         completed / success
Selected Shape Consumer Scan       completed / success
Report Store Consumer Scan         completed / success
```

If any required workflow has no exact-SHA push run, is still running, or is not successful, RC packaging stops with failure.

## Permissions

The workflow has read-only access:

```text
contents: read
actions: read
```

It must not request `contents: write` or `actions: write` and still contains no tag/GitHub Release creation path.

## Relationship to F5-C

F5-C added `push -> main` triggers to both standalone consumer scans. F5-D consumes those exact-main push results as a release prerequisite. Together the two packages make the release gate both observable and enforced.

## Frozen engineering/release behavior

F5-D changes release authorization only. It does not change:

- application production code;
- solver or engineering equations;
- selected F1/F2/F3/F4 authority;
- selected X/Z geometry;
- canonical engineering regression baseline;
- deterministic RC packaging bytes/procedure;
- `0.20 m` segmentation;
- signed feedback budget `64`;
- signed `WeightWaterKgM` semantics;
- exact deterministic fixed-point rule;
- persistence;
- PDF/2D engineering ownership;
- 3D status.

## Final gate after merge

After F5-D merges, use the new exact `main` SHA. Wait for its three push workflows to be green, create/update `release-candidate/v1.0.0` at that same SHA, and let the release workflow independently verify those gates before building the RC evidence set.

The workflow still must not create `v1.0.0` tag or GitHub Release. Final release remains blocked until the user manually tests that exact RC ZIP on Windows 11 and explicitly approves it.
