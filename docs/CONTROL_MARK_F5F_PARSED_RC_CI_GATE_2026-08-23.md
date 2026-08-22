# Control Mark — F5-F parsed RC CI gate

Date: 2026-08-23
Parent: #522
Issue: #561

## Trigger

The first traceable RC attempt from exact main `e57f6dc1bd2e4a941148ee0aa1f0438e13b9d6be` produced workflow run `32606049359` and correctly published `BuoyCalc Windows RC: failure`.

The job verified that the checked-out source was exact main, then failed before CI-gate evaluation or packaging because inline PowerShell parsed `$sourceSha:` as an invalid variable reference. The deterministic package, manifest, checksum and artifact-upload steps were skipped.

## F5-F boundary

The exact-main CI authorization logic is moved out of workflow YAML into:

`scripts/verify-exact-main-ci-gates.ps1`

The script requires:

- repository in `owner/name` form;
- a full exact source commit SHA;
- GitHub Actions read token;
- push runs for the exact source SHA;
- `.NET Build` completed/success;
- `Selected Shape Consumer Scan` completed/success;
- `Report Store Consumer Scan` completed/success.

The workflow calls this script before any RC package creation.

## Static protection

`tools/check-windows-rc-packaging.ps1` now parses the exact-main gate script with the PowerShell language parser in normal `.NET Build` CI. It also requires the three workflow names, exact SHA identity, push-event filter and completed/success contract, and forbids moving the Actions REST query back into inline workflow PowerShell.

## Unchanged engineering/release contents

F5-F does not change:

- solver or engineering physics;
- selected F1/F2/F3/F4 authority;
- selected X/Z geometry;
- canonical regression baseline;
- 0.20 m segmentation;
- signed feedback budget 64;
- signed `WeightWaterKgM` semantics;
- exact fixed-point acceptance;
- publish/package/manifest/checksum algorithms or RC package bytes for identical build inputs;
- persistence, PDF/2D behavior or 3D;
- tag or GitHub Release state.

No RC artifact was produced by the failed run, so there is no candidate artifact to revoke.
