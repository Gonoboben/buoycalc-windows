#!/usr/bin/env python3
"""Check actual selected authority, retaining the unmodified legacy discrepancy register."""
import collections
import json
import math
import pathlib
import sys

root = pathlib.Path(sys.argv[1])
rows = json.loads((root / "series-a-results.json").read_text(encoding="utf-8-sig"))
accepted = {"A05", "A06", "A10", "A11", "A14", "A15", "A17", "A18", "A20"}
indeterminate = {"A02", "A08"}
if len(rows) != 20 or {r["Id"] for r in rows} != {f"A{i:02}" for i in range(1, 21)}:
    raise ValueError("Series A coverage changed")
dispositions = []
for row in rows:
    expected = "Accepted" if row["Id"] in accepted else "Indeterminate" if row["Id"] in indeterminate else "RejectedPhysical"
    if row["SignedStatus"] != expected or row["ExceptionType"] is not None:
        raise ValueError(f"{row['Id']}: unexpected production status/exception")
    if row["MaxProductionSegmentLengthM"] > 0.20 + 1e-12 or abs(row["ProductionSegmentLengthSumM"] - row["LineLengthM"]) > 1e-9:
        raise ValueError(f"{row['Id']}: segmentation/arc length changed")
    authority = [row[k] for k in ("F1Available", "F2Available", "F3Available", "F4Available")]
    if expected == "Accepted":
        if row["SelectedShapeSource"] != "SignedBoundaryFeedback":
            raise ValueError(f"{row['Id']}: Accepted did not select the genuine signed authority")
        if not all(authority) or not row["SignedExactFixedPointReached"] or row["SignedFeedbackIterations"] > 64:
            raise ValueError(f"{row['Id']}: actual Accepted authority/fixed point missing")
        if row["SelectedEndpointChordWithinLine"] is not True or not all(math.isfinite(row[k]) for k in ("SelectedEndpointXM", "SelectedEndpointZM", "F1DemandN")):
            raise ValueError(f"{row['Id']}: selected geometry or force is invalid")
    elif any(authority):
        raise ValueError(f"{row['Id']}: non-Accepted has selected F1-F4")
    if expected == "RejectedPhysical" and row["SelectedEndpointXM"] is not None:
        raise ValueError(f"{row['Id']}: physical rejection exposes selected geometry")
    for finding in row["Findings"]:
        code = finding["Code"]
        if code in ("FallbackChordExceedsLineLength", "IterativeChordExceedsLineLength"):
            if expected == "Accepted" and not row["SelectedEndpointChordWithinLine"]:
                raise ValueError("diagnostic shape finding also affects selected geometry")
            classification = "NONSELECTED_DIAGNOSTIC_GEOMETRY_LIMITATION"
        elif code == "PhysicalRejectionNotPromotedToF4HardFailure" and expected == "RejectedPhysical":
            classification = "LEGACY_EXPECTATION_USES_F4_INSTEAD_OF_TERMINAL_PHYSICAL_DISPOSITION"
        else:
            raise ValueError(f"unexplained Series A finding: {row['Id']}/{code}")
        dispositions.append({"Id": row["Id"], "RawFinding": finding, "Disposition": classification})
report = {
    "Statuses": dict(collections.Counter(r["SignedStatus"] for r in rows)),
    "SelectedAuthorityChecks": "PASS", "RawFindingCount": len(dispositions),
    "RawFindingsRetained": True, "PhysicalValidation": "NOT_ESTABLISHED_BY_THIS_COMPUTATIONAL_CHECK",
    "Note": "Nonselected fallback limitations are retained, not repaired or reclassified as physical PASS. Physical terminal behavior is independently gated by Validation601 and typed report regressions.",
    "Dispositions": dispositions,
}
(root / "selected-authority-review.json").write_text(json.dumps(report, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
print(json.dumps({k: v for k, v in report.items() if k != "Dispositions"}, ensure_ascii=False))
