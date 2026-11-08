#!/usr/bin/env python3
"""Check actual selected authority, retaining the unmodified legacy discrepancy register."""
import collections
import json
import math
import pathlib
import sys

root = pathlib.Path(sys.argv[1])
rows = json.loads((root / "series-a-results.json").read_text(encoding="utf-8-sig"))
policy_file = pathlib.Path(__file__).resolve().parents[1] / (
    "validation/BuoyCalc.ValidationCampaign/baselines/series-a-known-findings.json"
)
policy = json.loads(policy_file.read_text(encoding="utf-8-sig"))
if policy["SchemaVersion"] != 1:
    raise ValueError("Unknown Series A finding policy schema")
allowed = {}
for entry in policy["Findings"]:
    for scenario in entry["ScenarioIds"]:
        key = (scenario, entry["Code"], entry["Severity"], entry["Category"])
        if key in allowed:
            raise ValueError(f"Duplicate Series A finding policy entry: {key}")
        allowed[key] = entry["Disposition"]
accepted = {"A05", "A06", "A10", "A11", "A14", "A15", "A17", "A18", "A20"}
indeterminate = {"A02", "A08"}
if len(rows) != 20 or {r["Id"] for r in rows} != {f"A{i:02}" for i in range(1, 21)}:
    raise ValueError("Series A coverage changed")
dispositions = []
seen = set()
for row in rows:
    expected = "Accepted" if row["Id"] in accepted else "Indeterminate" if row["Id"] in indeterminate else "RejectedPhysical"
    if row["SignedStatus"] != expected or row["ExceptionType"] is not None:
        raise ValueError(f"{row['Id']}: unexpected production status/exception")
    if row["MaxProductionSegmentLengthM"] > 0.20 + 1e-12 or abs(row["ProductionSegmentLengthSumM"] - row["LineLengthM"]) > 1e-9:
        raise ValueError(f"{row['Id']}: segmentation/arc length changed")
    authority = [row[k] for k in ("F1Available", "F2Available", "F3Available", "F4Available")]
    selected_fields = ("SelectedEndpointXM", "SelectedEndpointZM", "SelectedEndpointChordM")
    if row["SelectedShapeSource"] is not None:
        if row["SelectedEndpointChordWithinLine"] is not True or not all(
            math.isfinite(row[k]) for k in selected_fields
        ):
            raise ValueError(f"{row['Id']}: invalid selected geometry")
    elif any(row[k] is not None for k in selected_fields):
        raise ValueError(f"{row['Id']}: selected geometry has no source")
    if expected == "Accepted":
        if row["SelectedShapeSource"] != "SignedBoundaryFeedback":
            raise ValueError(f"{row['Id']}: Accepted did not select the genuine signed authority")
        if not all(authority) or not row["SignedExactFixedPointReached"] or not 1 <= row["SignedFeedbackIterations"] <= 64:
            raise ValueError(f"{row['Id']}: actual Accepted authority/fixed point missing")
        if not math.isfinite(row["F1DemandN"]):
            raise ValueError(f"{row['Id']}: selected force is invalid")
    elif any(authority):
        raise ValueError(f"{row['Id']}: non-Accepted has selected F1-F4")
    if expected == "RejectedPhysical" and row["SelectedShapeSource"] is not None:
        raise ValueError(f"{row['Id']}: physical rejection exposes selected geometry")
    for finding in row["Findings"]:
        code = finding["Code"]
        key = (row["Id"], code, finding["Severity"], finding["Category"])
        if key not in allowed or key in seen:
            raise ValueError(f"Unexpected or duplicate Series A finding: {key}")
        seen.add(key)
        if code in ("FallbackChordExceedsLineLength", "IterativeChordExceedsLineLength"):
            if expected not in ("Accepted", "RejectedPhysical"):
                raise ValueError("Diagnostic shape disposition requires signed selection or a physical block")
            classification = "NONSELECTED_DIAGNOSTIC_GEOMETRY_LIMITATION"
        elif code == "PhysicalRejectionNotPromotedToF4HardFailure" and expected == "RejectedPhysical":
            classification = "LEGACY_EXPECTATION_USES_F4_INSTEAD_OF_TERMINAL_PHYSICAL_DISPOSITION"
        else:
            raise ValueError(f"unexplained Series A finding: {row['Id']}/{code}")
        if allowed[key] != classification:
            raise ValueError(f"Incorrect Series A finding disposition: {key}")
        dispositions.append({"Id": row["Id"], "RawFinding": finding, "Disposition": classification})
if seen != allowed.keys():
    raise ValueError(f"Missing declared Series A findings: {sorted(allowed.keys() - seen)}")
report = {
    "Statuses": dict(collections.Counter(r["SignedStatus"] for r in rows)),
    "SelectedAuthorityChecks": "PASS", "RawFindingCount": len(dispositions),
    "FindingPolicy": {
        "SourceCommit": policy["SourceCommit"],
        "SourceArtifactId": policy["SourceArtifactId"],
        "SourceArtifactSHA256": policy["SourceArtifactSHA256"],
        "VerifiedOccurrences": len(seen),
    },
    "RawFindingsRetained": True, "PhysicalValidation": "NOT_ESTABLISHED_BY_THIS_COMPUTATIONAL_CHECK",
    "Note": "Nonselected fallback limitations are retained, not repaired or reclassified as physical PASS. Physical terminal behavior is independently gated by Validation601 and typed report regressions.",
    "Dispositions": dispositions,
}
(root / "selected-authority-review.json").write_text(json.dumps(report, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
print(json.dumps({k: v for k, v in report.items() if k != "Dispositions"}, ensure_ascii=False))
