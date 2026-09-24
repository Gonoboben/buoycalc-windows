# Physical Input Validation Contract

BC-AUD-004 defines one authoritative application-level validation gate:

`typed engineering input -> EngineeringInputValidator -> CurrentProfileRequirement -> InputHash -> BuoyCalculator.Calculate -> snapshot/provenance`

`ApplicationCalculationRunner.Run(...)` invokes the gate before input hashing and before the calculation core. A validation failure throws `EngineeringInputValidationException` with a stable code, field path, supplied value, and user-readable diagnostic. No `ApplicationCalculationRun`, `CalculationSnapshot`, or `CalculationRunProvenance` is created.

Raw calculation fields use `EngineeringNumberParser`. Syntax errors and non-finite values are blocking failures; they are never converted to zero, an absolute value, a preset default, or another correction value.

## Domain matrix

| Input | Finite | Zero | Negative | Rule |
|---|---:|---:|---:|---|
| Base water density | required | blocked | blocked | `> 0` |
| Project depth | required | blocked | blocked | `> 0` |
| Legacy/profile-derived scalar current magnitude | required | allowed | blocked | `>= 0`; not independent production authority |
| Wave height | required | allowed | blocked | `>= 0` |
| Wave period | required | allowed only when wave height is zero | blocked | `T > 0` when `H > 0` |
| Current-profile depth | required | allowed | blocked | `>= 0` |
| East/North/Vertical current component | required | allowed | **allowed** | signed direction is preserved |
| Profile-point water density | required | allowed | blocked | `0` retains the existing base-density fallback meaning |
| Seabed holding multiplier | required | allowed | blocked | raw nonnegative coefficient only; derived anchor prerequisites are out of scope |
| Buoy volume | required | blocked | blocked | `> 0` |
| Buoy mass, projected area, Cd | required | allowed | blocked | `>= 0` |
| Active line length | required | blocked | blocked | `> 0` |
| Rope diameter | required | blocked | blocked | `> 0` |
| Rope/connector MBL | required | blocked | blocked | `> 0` |
| Rope `WeightWaterKgM` | required | allowed | **allowed** | signed buoyant-line semantics are preserved |
| Rope Cd | required | allowed | blocked | `>= 0` |
| Connector mass, volume, projected area, Cd | required | allowed | blocked | `>= 0` |
| Payload mass, volume, projected area, Cd | required | allowed | blocked | `>= 0` |
| Anchor mass, volume, base holding coefficient | required | allowed | blocked | raw values `>= 0`; derived submerged-weight authority is BC-AUD-010 |
| Enabled element count | integer | blocked | blocked | `> 0` |
| Safety factor | required | blocked | blocked | `> 0` |

Duplicate current-profile depths remain governed by the existing current-profile contract and BC-AUD-007. Short-line rejection, fixed-point convergence, selected-shape arbitration, derived anchor submerged-weight prerequisites, and all engineering formulas are unchanged.
