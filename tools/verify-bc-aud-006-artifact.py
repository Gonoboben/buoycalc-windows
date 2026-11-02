#!/usr/bin/env python3
"""Independently verify emitted BC-AUD-006 bytes, never solver acceptance."""
import argparse
import csv
import hashlib
import io
import itertools
import json
import zipfile


def rows(archive, name):
    return csv.DictReader(io.TextIOWrapper(archive.open(name), encoding="utf-8-sig", newline=""))


def verify(archive, fields, manifest, grouping, names):
    expected = {}
    for row in rows(archive, manifest):
        key = tuple(row[k] for k in grouping)
        if key in expected:
            raise ValueError(f"duplicate manifest key: {key}")
        expected[key] = row
    states = count = 0
    for key, group in itertools.groupby(rows(archive, fields), lambda r: tuple(r[k] for k in grouping)):
        digest = hashlib.sha256()
        field_names = set()
        field_count = 0
        for row in group:
            name = "/".join(row[k] for k in names)
            if name in field_names:
                raise ValueError(f"duplicate field in {key}: {name}")
            field_names.add(name)
            digest.update(f"{name}={row['RawBits']}\n".encode("utf-8"))
            field_count += 1
        item = expected.pop(key)
        if digest.hexdigest().upper() != item["SHA256"] or field_count != int(item["FieldCount"]):
            raise ValueError(f"state digest/count mismatch: {key}")
        states += 1
        count += field_count
    if expected:
        raise ValueError("missing states")
    return {"verified_hashes": states, "field_rows": count}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("artifact")
    parser.add_argument("--output")
    args = parser.parse_args()
    with open(args.artifact, "rb") as stream:
        digest = hashlib.file_digest(stream, "sha256").hexdigest()
    result = {"artifact_sha256": digest}
    with zipfile.ZipFile(args.artifact) as archive:
        for label, prefix in (("production", "parity-production"), ("mirror", "parity-readout")):
            result[label] = verify(archive, f"{prefix}-fields.csv", f"{prefix}-step-sha256.csv",
                                   ["Scenario", "Iteration"], ["Stage", "SegmentNumber", "Field"])
        compared = 0
        for production, mirror in itertools.zip_longest(rows(archive, "parity-production-fields.csv"),
                                                       rows(archive, "parity-readout-fields.csv")):
            if production != mirror:
                raise ValueError("production/mirror raw fields differ")
            compared += 1
        result["parity_exact_equal_fields"] = compared
        with zipfile.ZipFile(io.BytesIO(archive.read("root-cause-isolation-work.zip"))) as research:
            result["research"] = verify(research, "full-states.csv", "state-sha256.csv",
                                         ["Scenario", "Mode", "Iteration"], ["Field"])
            result["outcomes"] = list(rows(research, "outcomes.csv"))
            if any(r["ObservedSteps"] != "64" or r["ReplayIdentical"] != "True" for r in result["outcomes"]):
                raise ValueError("incomplete or non-replayed trajectory")
        result["observer_controls"] = list(rows(archive, "parity-observer-controls.csv"))
    output = json.dumps(result, indent=2) + "\n"
    if args.output:
        with open(args.output, "w", encoding="utf-8") as stream:
            stream.write(output)
    print(output, end="")


if __name__ == "__main__":
    main()
