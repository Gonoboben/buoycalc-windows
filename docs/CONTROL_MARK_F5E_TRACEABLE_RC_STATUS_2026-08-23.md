# Control Mark — F5-E traceable RC build status

Date: 2026-08-23
Issue: #559
Parent: #522
Starting exact main: `3d1bb11a2fdc564f8256e7abfb0eebb788a5d80a`

## Purpose

Attach the actual Windows Release Candidate workflow run to its exact source commit through one stable classic commit-status context.

## Status context

The release workflow publishes exactly:

```text
BuoyCalc Windows RC
```

on `github.sha`.

The context is:

- `pending` when the RC workflow begins;
- `success` only when the job has completed the exact-main source check, machine-enforced CI gate, deterministic package creation, independent manifest/SHA-256 verification, evidence summary and artifact upload;
- `failure` when any preceding release job step fails.

The status `target_url` is the exact Actions run:

```text
${{ github.server_url }}/${{ github.repository }}/actions/runs/${{ github.run_id }}
```

This provides a stable machine-readable pointer from the tested source commit to the RC workflow and artifact.

## Permissions

The release workflow permissions remain narrowly scoped:

```text
contents: read
actions: read
statuses: write
```

`statuses: write` can only add commit-status evidence. The workflow does not receive repository-content, Actions, issue, package, deployment, check or pull-request write permission.

## Frozen behavior

F5-E does not change:

- production application code;
- solver/engineering physics;
- selected F1/F2/F3/F4 authority;
- selected X/Z geometry;
- canonical engineering baseline;
- publish/package/verification scripts;
- RC ZIP bytes or deterministic packaging procedure;
- persistence;
- PDF/2D behavior;
- 3D status.

It also adds no git-tag or GitHub-Release creation path.

## Final RC procedure

After F5-E merge:

1. require the new exact main SHA to pass `.NET Build`, `Selected Shape Consumer Scan`, `Report Store Consumer Scan`, and classic `BuoyCalc Windows Build`;
2. fast-forward `release-candidate/v1.0.0` to that same SHA;
3. require `BuoyCalc Windows RC: success` on that source commit;
4. follow its target URL to the exact Actions run;
5. verify/download the `BuoyCalc-Windows-v1.0.0-win-x64-RC` artifact and its ZIP/checksum/manifest;
6. stop for the user's manual Windows 11 smoke of that exact ZIP.

No final `v1.0.0` tag or GitHub Release is permitted before explicit user approval after the smoke test.
