# Project engineering-input snapshot

The v1 project JSON is an input-only persistence format. It does not retain a
calculation result, `RunId`, or BC-AUD-005 run provenance. Loading a project clears
the current calculation authority and requires an explicit Calculate.

## Replay authority

Buoy, anchor, and payload engineering values are stored directly in the project and
are authoritative on restore. Their selected preset IDs are library/source hints;
selecting a current library item during restore must not overwrite the stored raw
values.

Rope and connector calculation inputs are normally library-backed. Each newly saved
assembly item therefore stores both its source preset ID and an embedded resolved
engineering snapshot:

- rope: ID, name, material, diameter, MBL, signed water weight per metre and Cd;
- connector: ID, name, type, air weight, volume, MBL, projected area and Cd.

Library notes and other presentation-only fields are excluded. The embedded values
used at save time become project replay authority. Renaming, modifying or deleting
the library item cannot silently change the saved project.

The source reference ID and embedded snapshot ID must match. A mismatch is an
explicit load error rather than a choice between two authorities.

## Legacy compatibility

The two embedded snapshot properties are optional so existing JSON continues to
deserialize. For a legacy line or connector without an embedded snapshot:

- if the exact referenced preset still exists, it is resolved and calculation may
  continue; the next save embeds that resolved engineering input;
- if the reference is missing, load is blocked with a diagnostic naming the missing
  ID;
- no first-item, built-in, or generic fallback is permitted.

Unknown payload references do not resolve another payload: the raw payload values
already stored in the project remain authoritative. Buoy and anchor restore follows
the same raw-value ownership rule.
