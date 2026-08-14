using System.Globalization;
using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Services;

internal static class BoundaryConditionedSignedGeometryRegression
{
    private const double IdentityToleranceM = 1e-9;
    private const double ForceToleranceN = 1e-9;
    private const double UnitVectorTolerance = 1e-12;
    private const double DirectionTolerance = 1e-12;

    public static void Validate()
    {
        ValidateCanonicalIdentity();
        ValidateControlledMechanicalDirections();
        ValidateControlledBuoyantPointJump();
        ValidateIndeterminateResultant();
    }

    private static void ValidateCanonicalIdentity()
    {
        foreach (var scenario in SurfaceBoundaryCanonicalMeasurementRegression.BuildCanonicalScenarios())
        {
            var run = ApplicationCalculationRunner.Run(
                scenario.Environment,
                scenario.Buoy,
                scenario.Assembly,
                scenario.Anchor,
                scenario.SafetyFactor);
            var data = run.Snapshot.TechnicalReportData;
            var parent = data.SurfaceBoundaryInfo;
            var trace = data.SurfaceBoundaryTensionTrace;

            if (!parent.Solved || parent.SolutionState is null)
            {
                throw new InvalidOperationException(
                    $"Boundary-conditioned signed geometry {scenario.Label}: canonical parent must be solved; got {parent.Classification}.");
            }

            if (!trace.Available)
            {
                throw new InvalidOperationException(
                    $"Boundary-conditioned signed geometry {scenario.Label}: stored trace unavailable: {trace.UnavailableReason}");
            }

            if (trace.Rows.Count != run.Result.SegmentRows.Count)
            {
                throw new InvalidOperationException(
                    $"Boundary-conditioned signed geometry {scenario.Label}: trace row count {trace.Rows.Count} != segment count {run.Result.SegmentRows.Count}.");
            }

            if (trace.PointLoadCrossings != parent.SolutionState.PointLoadCrossings)
            {
                throw new InvalidOperationException(
                    $"Boundary-conditioned signed geometry {scenario.Label}: point-load ownership changed ({trace.PointLoadCrossings} != {parent.SolutionState.PointLoadCrossings}).");
            }

            Near(parent.SolutionState.EndHN, Require(trace.EndHN, scenario.Label + " terminal H"), ForceToleranceN, scenario.Label + " terminal H");
            Near(parent.SolutionState.EndVN, Require(trace.EndVN, scenario.Label + " terminal V"), ForceToleranceN, scenario.Label + " terminal V");

            var reconstructed = Reconstruct(trace.Rows, scenario.Label);
            if (!reconstructed.Available || reconstructed.IndeterminateSegmentCount != 0)
            {
                throw new InvalidOperationException(
                    $"Boundary-conditioned signed geometry {scenario.Label}: canonical reconstruction unexpectedly indeterminate.");
            }

            Near(parent.SolutionState.EndpointXM, reconstructed.EndpointXM, IdentityToleranceM, scenario.Label + " endpoint X identity");
            Near(parent.SolutionState.EndpointZM, reconstructed.EndpointZM, IdentityToleranceM, scenario.Label + " endpoint Z identity");

            var selectedX = run.Snapshot.SelectedShape?.Shape.HorizontalOffsetM;
            Console.WriteLine(string.Join("|",
                "BOUNDARY_CONDITIONED_SIGNED_GEOMETRY",
                scenario.Label,
                $"TraceX={Format(reconstructed.EndpointXM)}",
                $"TraceZ={Format(reconstructed.EndpointZM)}",
                $"ParentX={Format(parent.SolutionState.EndpointXM)}",
                $"ParentZ={Format(parent.SolutionState.EndpointZM)}",
                $"NegativeDz={reconstructed.NegativeDzSegmentCount}",
                $"PointLoads={trace.PointLoadCrossings}",
                $"SelectedX={Format(selectedX)}",
                $"BoundaryMinusSelectedX={Format(selectedX.HasValue ? reconstructed.EndpointXM - selectedX.Value : null)}"));
        }
    }

    private static void ValidateControlledMechanicalDirections()
    {
        var downward = MechanicalRow(
            segmentNumber: 1,
            startLengthM: 0.0,
            endLengthM: 1.0,
            crossingsBefore: 0,
            startH: 3.0,
            startV: 4.5,
            midH: 3.0,
            midV: 4.0,
            endH: 3.0,
            endV: 3.5);
        var downwardGeometry = Reconstruct(new[] { downward }, "controlled downward");
        if (!downwardGeometry.Available || downward.TangentZ is null || downward.TangentZ.Value <= 0.0 || downwardGeometry.EndpointZM <= 0.0)
        {
            throw new InvalidOperationException(
                "Boundary-conditioned signed geometry controlled downward case must preserve V>0 as TangentZ>0 and dz>0.");
        }

        // Mechanically defined buoyant distributed load: V increases from -3 N to -1 N
        // along +s, consistent with V_after = V_before - W_water*g for signed W_water < 0.
        var buoyantUpward = MechanicalRow(
            segmentNumber: 1,
            startLengthM: 0.0,
            endLengthM: 1.0,
            crossingsBefore: 0,
            startH: 3.0,
            startV: -3.0,
            midH: 3.0,
            midV: -2.0,
            endH: 3.0,
            endV: -1.0);
        var upwardGeometry = Reconstruct(new[] { buoyantUpward }, "controlled buoyant distributed");
        if (buoyantUpward.EndVN <= buoyantUpward.StartVN)
        {
            throw new InvalidOperationException(
                "Boundary-conditioned signed geometry controlled buoyant distributed case must increase V along +s for negative signed water weight.");
        }
        if (!upwardGeometry.Available || buoyantUpward.TangentZ is null || buoyantUpward.TangentZ.Value >= 0.0 || upwardGeometry.EndpointZM >= 0.0 || upwardGeometry.NegativeDzSegmentCount != 1)
        {
            throw new InvalidOperationException(
                "Boundary-conditioned signed geometry controlled buoyant distributed case must preserve V<0 as TangentZ<0 and dz<0.");
        }
    }

    private static void ValidateControlledBuoyantPointJump()
    {
        var beforePoint = MechanicalRow(
            segmentNumber: 1,
            startLengthM: 0.0,
            endLengthM: 1.0,
            crossingsBefore: 0,
            startH: 2.0,
            startV: 5.0,
            midH: 2.0,
            midV: 4.5,
            endH: 2.0,
            endV: 4.0);
        var afterBuoyantPoint = MechanicalRow(
            segmentNumber: 2,
            startLengthM: 1.0,
            endLengthM: 2.0,
            crossingsBefore: 1,
            startH: 3.0,
            startV: 6.0,
            midH: 3.0,
            midV: 5.5,
            endH: 3.0,
            endV: 5.0);

        var deltaH = afterBuoyantPoint.StartHN - beforePoint.EndHN;
        var deltaV = afterBuoyantPoint.StartVN - beforePoint.EndVN;
        if (deltaH <= 0.0)
        {
            throw new InvalidOperationException(
                "Boundary-conditioned signed geometry controlled point jump must preserve positive point current-force contribution in H.");
        }
        if (deltaV <= 0.0)
        {
            throw new InvalidOperationException(
                "Boundary-conditioned signed geometry controlled buoyant point jump must increase V, preserving negative signed point water weight.");
        }
        if (afterBuoyantPoint.PointLoadCrossingsAppliedBeforeSegment != 1)
        {
            throw new InvalidOperationException(
                "Boundary-conditioned signed geometry controlled point jump must be crossed exactly once.");
        }

        var geometry = Reconstruct(new[] { beforePoint, afterBuoyantPoint }, "controlled buoyant point");
        if (!geometry.Available || geometry.IndeterminateSegmentCount != 0)
        {
            throw new InvalidOperationException(
                "Boundary-conditioned signed geometry controlled buoyant point trace must remain geometrically available.");
        }
    }

    private static void ValidateIndeterminateResultant()
    {
        var zero = MechanicalRow(
            segmentNumber: 1,
            startLengthM: 0.0,
            endLengthM: 1.0,
            crossingsBefore: 0,
            startH: 0.0,
            startV: 0.0,
            midH: 0.0,
            midV: 0.0,
            endH: 0.0,
            endV: 0.0);
        var geometry = Reconstruct(new[] { zero }, "controlled zero resultant");

        if (geometry.Available || geometry.IndeterminateSegmentCount != 1)
        {
            throw new InvalidOperationException(
                "Boundary-conditioned signed geometry zero-resultant case must be indeterminate rather than manufacturing a tangent.");
        }
        Near(0.0, geometry.EndpointXM, DirectionTolerance, "zero-resultant X must not be manufactured");
        Near(0.0, geometry.EndpointZM, DirectionTolerance, "zero-resultant Z must not be manufactured");
    }

    private static Reconstruction Reconstruct(
        IReadOnlyList<MooringSurfaceBoundaryTensionTraceRow> rows,
        string label)
    {
        var x = 0.0;
        var z = 0.0;
        var negativeDz = 0;
        var indeterminate = 0;

        foreach (var row in rows)
        {
            var ds = row.EndLengthM - row.StartLengthM;
            if (!double.IsFinite(ds) || ds < -DirectionTolerance)
            {
                throw new InvalidOperationException(
                    $"Boundary-conditioned signed geometry {label}: invalid segment length for row {row.SegmentNumber}: {ds:R}.");
            }

            if (!row.TangentX.HasValue || !row.TangentZ.HasValue)
            {
                indeterminate++;
                continue;
            }

            var tx = row.TangentX.Value;
            var tz = row.TangentZ.Value;
            if (!double.IsFinite(tx) || !double.IsFinite(tz))
            {
                throw new InvalidOperationException(
                    $"Boundary-conditioned signed geometry {label}: non-finite tangent on row {row.SegmentNumber}.");
            }

            Near(1.0, tx * tx + tz * tz, UnitVectorTolerance, label + $" row {row.SegmentNumber} tangent norm");

            var dx = ds * tx;
            var dz = ds * tz;
            x += dx;
            z += dz;
            if (dz < -DirectionTolerance)
                negativeDz++;
        }

        return new Reconstruction(
            indeterminate == 0,
            x,
            z,
            negativeDz,
            indeterminate);
    }

    private static MooringSurfaceBoundaryTensionTraceRow MechanicalRow(
        int segmentNumber,
        double startLengthM,
        double endLengthM,
        int crossingsBefore,
        double startH,
        double startV,
        double midH,
        double midV,
        double endH,
        double endV)
    {
        var tension = Math.Sqrt(midH * midH + midV * midV);
        double? tx = null;
        double? tz = null;
        double? angle = null;
        if (double.IsFinite(tension) && tension > 1e-9)
        {
            tx = midH / tension;
            tz = midV / tension;
            angle = Math.Atan2(midH, midV) * 180.0 / Math.PI;
        }

        return new MooringSurfaceBoundaryTensionTraceRow(
            segmentNumber,
            $"Controlled {segmentNumber}",
            startLengthM,
            endLengthM,
            (startLengthM + endLengthM) / 2.0,
            0.0,
            crossingsBefore,
            startH,
            startV,
            midH,
            midV,
            endH,
            endV,
            tension,
            tx,
            tz,
            angle);
    }

    private static double Require(double? value, string label)
    {
        return value ?? throw new InvalidOperationException(
            $"Boundary-conditioned signed geometry: missing {label}.");
    }

    private static void Near(double expected, double actual, double tolerance, string label)
    {
        if (!double.IsFinite(actual) || Math.Abs(expected - actual) > tolerance)
        {
            throw new InvalidOperationException(
                $"Boundary-conditioned signed geometry {label}: expected {expected:R}, got {actual:R}, tolerance {tolerance:R}.");
        }
    }

    private static string Format(double? value)
    {
        return value.HasValue
            ? value.Value.ToString("R", CultureInfo.InvariantCulture)
            : "n/a";
    }

    private sealed record Reconstruction(
        bool Available,
        double EndpointXM,
        double EndpointZM,
        int NegativeDzSegmentCount,
        int IndeterminateSegmentCount);
}
