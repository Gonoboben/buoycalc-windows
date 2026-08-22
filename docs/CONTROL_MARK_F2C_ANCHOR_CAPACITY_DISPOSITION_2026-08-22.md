# Control mark — F2-C anchor holding-capacity disposition — 2026-08-22

Parent milestone: #522  
Issue: #536

## Scope

This checkpoint validates whether the existing anchor holding-capacity scalar may become authority after the selected signed anchor-end reaction/contact path introduced in F2-A/F2-B.

No production anchor reserve, checks, verdict, solver, geometry, wave, weak-link, persistence or renderer behavior changes in this package.

## Selected anchor-boundary state already validated

For an Accepted `SignedBoundaryFeedback` design envelope:

```text
s = 0                         buoy / surface
s = L                         anchor / seabed
+Z                            downward
InternalAnchorEnd = (H, V)    top-to-bottom internal line state
LineOnAnchor = (-H, -V)
HorizontalDemand = |H|
Wsubmerged = AnchorWeightWaterKg * g
SignedNormalReaction = Wsubmerged - V
CompressiveNormalReaction = max(0, Wsubmerged - V)
UpliftExcess = max(0, V - Wsubmerged)
```

The selected reaction/contact state does not itself define soil or anchor holding capacity.

## Existing legacy capacity

Production `CalculationResult` currently owns:

```text
AnchorHoldingKg =
  AnchorWeightWaterKg
  * AnchorBaseHoldingCoefficient
  * AnchorTypeMultiplier
  * SeabedHoldingMultiplier

RequiredAnchorHoldingKg = HorizontalForceN / g
AnchorReserve = AnchorHoldingKg / RequiredAnchorHoldingKg
```

The regression proves this exact algebra for all canonical scenarios.

Current catalog provenance is intentionally weak for physical reinterpretation:

- built-in anchor presets are explicitly labelled educational (`Учебный ...`);
- seabed values are generic holding multipliers with notes such as conditional/moderate/reduced holding;
- neither `AnchorInput` nor `SeabedPreset` contains an explicit Coulomb friction coefficient;
- the same scalar structure is used for deadweight, mushroom and plate anchors even though their physical holding mechanisms differ.

Therefore the current product coefficients must not be silently renamed or treated as a validated `mu`.

## Independent deadweight reference

Primary project source: G. O. Berteaux, *Океанографические буи* (Russian translation of H. O. Berteaux, *Buoy Engineering*), Судостроение, 1979, anchor discussion around pp. 176–180.

For a deadweight anchor on a horizontal seabed, the force balance reduces to the expected friction relation:

```text
N = Wsubmerged - Vup
Hcapacity = mu * N
Wsubmerged_required = Vup + H / mu
```

where `Vup` is the upward line pull at the anchor and `mu` is a physical friction coefficient for the relevant anchor/seabed interface.

Consequences:

1. upward anchor-line pull reduces the available normal reaction and horizontal friction capacity;
2. at zero normal reaction the weight/friction capacity is zero;
3. after uplift/separation no positive Coulomb friction capacity may be claimed from seabed normal contact;
4. numerical equality between a generic legacy holding multiplier and a friction coefficient would not establish semantic equivalence.

Berteaux separately treats burying/special anchors as mechanisms whose horizontal and vertical resistance depends on anchor geometry and soil interaction; they cannot be validated from a single weight multiplier.

## Canonical evidence policy

For each Accepted signed-selected historical fixture the validation records side-by-side:

```text
legacy global horizontal demand
selected local anchor horizontal demand
selected anchor V
selected contact classification
selected compressive normal reaction
legacy holding capacity
legacy reserve
legacy generic holding-factor product
counterfactual normal-reaction * legacy-factor capacity
counterfactual reserve
```

The counterfactual values are evidence only. They are deliberately not used as an acceptance gate or production authority.

For non-Accepted selected geometry, no selected anchor-reaction/capacity authority is fabricated and legacy behavior remains the compatibility path.

## Authority disposition

```text
Selected anchor H/V/contact authority       = Validated where available
Legacy AnchorHoldingKg                      = CompatibilityOnly
Legacy RequiredAnchorHoldingKg              = CompatibilityOnly
Legacy AnchorReserve                        = CompatibilityOnly
Legacy generic holding factors as mu        = NotValidated
Deadweight selected horizontal capacity     = RequiresExplicitFrictionCoefficient
Mushroom/plate/embedment capacity            = RequiresAdditionalSoilEmbedmentModel
Production anchor-capacity migration        = NotAuthorized
```

This is a semantic decision, not a failed numerical-parity test.

## F2 conclusion

F2 has established a validated selected anchor-end force/reaction/contact boundary, but the present product does not contain enough validated anchor/soil capacity data to replace the legacy anchor reserve safely.

The correct v1 behavior at this checkpoint is to preserve `AnchorReserve` as a compatibility scalar and not let selected geometry authority silently transfer to anchor holding capacity.

A future production anchor-capacity package must first introduce a physically named capacity contract. For deadweight anchors that means an explicit validated friction/interface parameter (and any seabed-slope policy). Embedded/special anchors require their own validated anchor/soil model rather than a reuse of the deadweight relation.
