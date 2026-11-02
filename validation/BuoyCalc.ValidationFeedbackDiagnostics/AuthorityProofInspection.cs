using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

// Independent measurements of an already Accepted result, never a replacement acceptance gate.
internal static class AuthorityProofInspection
{
    internal static object Inspect(CalculationResult result, MooringSequencePositionResult sequence,
        MooringSignedCandidateResult candidate, string mode)
    {
        var shape = candidate.Shape!;
        var trace = candidate.FinalTensionTrace!;
        var boundary = candidate.Boundary!;
        Require(candidate.ExactFixedPointReached && candidate.FeedbackIterations <= 64, "Accepted without exact fixed point/budget.");
        Require(Bits(shape.AnchorPoint!.XOffsetM) == Bits(boundary.SolutionState!.EndpointXM) &&
            Bits(shape.AnchorPoint.ZDepthM) == Bits(boundary.SolutionState.EndpointZM), "Accepted endpoint raw bits differ from boundary.");
        Require(Bits(trace.StartHN!.Value) == Bits(boundary.BuoySteadyDragN!.Value) &&
            Bits(trace.StartVN!.Value) == Bits(boundary.Q0N!.Value) &&
            Bits(trace.EndHN!.Value) == Bits(boundary.SolutionState.EndHN) &&
            Bits(trace.EndVN!.Value) == Bits(boundary.SolutionState.EndVN), "Trace/boundary raw force bits differ.");
        var projection = MooringShapeProjection.Build(shape);
        var direct = MooringShapeForceAnalyzer.BuildWithDiagnosticTangents(result, projection, trace);
        var segments = result.SegmentRows.OrderBy(s => s.Number).ToArray();
        var points = sequence.Rows.OrderBy(s => s.Number).ToArray();
        var internalPoints = points.Skip(1).SkipLast(1).Where(p => p.IsDiscrete)
            .OrderBy(p => p.PositionAlongLineM).ThenBy(p => p.Number).ToArray();
        var maxLength = 0.0; var maxTangent = 0.0; var maxH = 0.0; var maxV = 0.0;
        var maxJump = 0.0; var unitNorm = 0.0; var previousH = trace.StartHN.Value;
        var previousV = trace.StartVN.Value; var pointIndex = 0;
        for (var i = 0; i < trace.Rows.Count; i++)
        {
            var row = trace.Rows[i]; var segment = segments[i]; var p = projection.Rows[i];
            Require(row.SegmentNumber == segment.Number && p.SegmentNumber == segment.Number, "Segment identity mismatch.");
            maxLength = Math.Max(maxLength, p.LengthResidualM);
            maxTangent = Math.Max(maxTangent, Math.Max(Math.Abs(p.DeltaXM / segment.SegmentLengthM - row.TangentX!.Value),
                Math.Abs(p.DeltaZM / segment.SegmentLengthM - row.TangentZ!.Value)));
            unitNorm = Math.Max(unitNorm, Math.Abs(row.TangentX!.Value * row.TangentX.Value + row.TangentZ!.Value * row.TangentZ.Value - 1));
            var jumpH = 0.0; var jumpV = 0.0;
            while (pointIndex < row.PointLoadCrossingsAppliedBeforeSegment)
            {
                jumpH += internalPoints[pointIndex].CurrentForceN;
                jumpV -= internalPoints[pointIndex++].WeightWaterKg * MooringSurfaceBoundaryIntegrationKernel.GravityMS2;
            }
            maxJump = Math.Max(maxJump, Math.Max(Math.Abs((row.StartHN - previousH) - jumpH), Math.Abs((row.StartVN - previousV) - jumpV)));
            maxH = Math.Max(maxH, Math.Abs((row.EndHN - row.StartHN) - direct.Rows[i].ShapeForceN));
            maxV = Math.Max(maxV, Math.Abs((row.EndVN - row.StartVN) + segment.WeightWaterKg * MooringSurfaceBoundaryIntegrationKernel.GravityMS2));
            if (mode is "H2" or "H1H2")
                Require(Bits(row.EndHN) == Bits(row.StartHN + direct.Rows[i].ShapeForceN), "H2 fixed-point segment H identity differs.");
            Require(Bits(row.EndVN) == Bits(row.StartVN - segment.WeightWaterKg * MooringSurfaceBoundaryIntegrationKernel.GravityMS2), "Signed submerged-weight update differs.");
            previousH = row.EndHN; previousV = row.EndVN;
        }
        var terminalJumpH = 0.0; var terminalJumpV = 0.0;
        while (pointIndex < internalPoints.Length)
        {
            terminalJumpH += internalPoints[pointIndex].CurrentForceN;
            terminalJumpV -= internalPoints[pointIndex++].WeightWaterKg * MooringSurfaceBoundaryIntegrationKernel.GravityMS2;
        }
        maxJump = Math.Max(maxJump, Math.Max(Math.Abs(trace.EndHN.Value - previousH - terminalJumpH), Math.Abs(trace.EndVN.Value - previousV - terminalJumpV)));
        Require(pointIndex == candidate.PointLoadCrossings && pointIndex == trace.PointLoadCrossings, "Discrete load crossings differ.");
        // Same published identity scales as evaluator/kernel. No feedback convergence tolerance.
        Require(maxJump <= 1e-6 && maxLength <= MooringSurfaceBoundaryIntegrationKernel.LengthToleranceM, "Existing point-load/geometry identity bound exceeded.");
        return new
        {
            EndpointRawBitsEqual = true, TraceBoundaryForceRawBitsEqual = true,
            MaxSegmentLengthResidualM = maxLength, TotalArcLengthResidualM = projection.LengthResidualM,
            MaxCoordinateTangentDelta = maxTangent, MaxTangentUnitNormResidual = unitNorm,
            MaxDirectTangentForceVsTraceResidualN = maxH, MaxSignedWeightVerticalForceResidualN = maxV,
            MaxDiscreteJumpResidualN = maxJump, PointLoadCrossings = pointIndex,
            DirectTangentGlobalHorizontalResidualN = trace.EndHN.Value - trace.StartHN.Value - direct.Rows.Sum(r => r.ShapeForceN) - internalPoints.Sum(p => p.CurrentForceN),
            GlobalVerticalResidualN = trace.EndVN.Value - trace.StartVN.Value + segments.Sum(s => s.WeightWaterKg) * MooringSurfaceBoundaryIntegrationKernel.GravityMS2 + internalPoints.Sum(p => p.WeightWaterKg) * MooringSurfaceBoundaryIntegrationKernel.GravityMS2,
            SignedLineWeightKg = segments.Sum(s => s.WeightWaterKg), MinSegmentLengthM = segments.Min(s => s.SegmentLengthM)
        };
    }

    private static long Bits(double value) => BitConverter.DoubleToInt64Bits(value);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
