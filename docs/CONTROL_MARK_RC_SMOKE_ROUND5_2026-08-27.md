# CONTROL MARK — RC smoke round 5

Date: 2026-08-27
Parent release gate: #522
Issue: #573

This UI-only release-candidate polish changes no engineering physics.

## Frozen behavior

- The left-column cards `Условия постановки`, `Буй`, and `Якорь и запас` are the only setup-card expanders and start collapsed.
- Starting a new project or invoking project load collapses those setup cards again; collapse state remains UI-only and is not persisted in project JSON.
- Sequence element editor cards also start collapsed when created/restored, so opening the application/project presents a compact sequence.
- Duplicated sequence cards start collapsed rather than inheriting an expanded editor state.
- `Проект` remains non-collapsible.
- `Проверить схему и рассчитать` is outside `Якорь и запас`, so it remains directly visible while setup cards are collapsed.
- The existing sequence-preview confirmation and `CalculateCommand` workflow are unchanged.

## Explicitly unchanged

- solver and engineering physics;
- F1–F4 authority semantics;
- PDF/report authority and content;
- project/library formats;
- production segmentation 0.20 m;
- signed feedback budget 64;
- signed `WeightWaterKgM` semantics;
- exact deterministic fixed-point candidate acceptance;
- no 3D.
