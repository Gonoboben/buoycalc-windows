# Control Mark — F5-B deterministic Windows RC packaging

Date: 2026-08-23
Parent milestone: #522
Issue: #553
Starting exact main: `ec782587f4ca1d4b300f498cc58a74a9d03fb6cf`

## Purpose

Freeze the Windows Release Candidate packaging/evidence contract without changing any engineering calculation behavior.

## RC identity

```text
version: v1.0.0
runtime: win-x64
configuration: Release
self-contained: true
single-file: true
```

## Evidence filenames

```text
BuoyCalc-Windows-v1.0.0-win-x64.zip
BuoyCalc-Windows-v1.0.0-win-x64.sha256
BuoyCalc-Windows-v1.0.0-win-x64-manifest.json
workflow artifact: BuoyCalc-Windows-v1.0.0-win-x64-RC
```

The manifest must record the exact source commit SHA, ZIP SHA-256 and executable SHA-256.

## Deterministic packaging procedure

For identical published executable bytes, the package procedure is normalized by:

- exactly one ZIP entry;
- stable entry name `BuoyCalc-Windows-v1.0.0-win-x64/BuoyCalc.Windows.exe`;
- store/no-compression ZIP entry;
- fixed ZIP entry timestamp `2000-01-01T00:00:00Z`;
- no filesystem enumeration ordering dependency;
- UTF-8 no-BOM checksum/manifest output;
- SHA-256 over the final ZIP.

This is a deterministic packaging contract; it does not claim code signing or cross-toolchain reproducibility of the published EXE.

## Exact-main source gate

The workflow remains manually dispatchable and also listens to:

```text
release-candidate/v1.0.0
```

Before packaging it requires:

```text
HEAD == github.sha
HEAD == origin/main
```

The RC trigger ref therefore cannot authorize a commit different from exact `main`.

## Frozen engineering state

F5-B must preserve the F5-A frozen engineering baseline blob:

```text
97b0221ab29d8df4c9f2f435a1ba1780033d318a
```

And must not change:

- solver or engineering equations;
- retained F1/F2/F3/F4 authority;
- selected X/Z geometry;
- production segment length exactly `0.20 m`;
- signed feedback budget exactly `64`;
- signed `WeightWaterKgM` semantics;
- Accepted-candidate exact deterministic fixed-point rule;
- persistence;
- PDF/2D engineering ownership;
- 3D status (post-v1 only).

## Release prohibition

F5-B creates only Release Candidate evidence. It must not create a git tag or GitHub Release.

After F5-B merge and exact-main green CI, build the RC from exact main, verify the evidence files, and stop for the user's manual Windows 11 smoke. Only explicit user approval after testing that same ZIP can authorize final `v1.0.0` tag/Release creation.
