internal static class PointLoadAnalyticalReferenceRegression
{
    private const double LengthM = 55.0;
    private const double TargetDepthM = 50.0;
    private const double H0N = 82.0;
    private const double QxNPerM = 3.075;
    private const double WeightNPerM = 0.980665;
    private const double QCapacityN = 9071.15125;

    private const double ConnectorDragN = 1.28125;
    private const double PayloadDragN = 6.40625;
    private const double ConnectorWeightWaterKg = 4.2825;
    private const double PayloadWeightWaterKg = 34.875;
    private const double G = 9.80665;

    private const double PointDragN = ConnectorDragN + PayloadDragN;
    private const double PointWeightWaterN =
        (ConnectorWeightWaterKg + PayloadWeightWaterKg) * G;

    private const double ExpectedExactQ0N = 741.2658404389103;
    private const double ExpectedExactXM = 19.48029920258567;

    private const double ForceEpsilonN = 1e-12;
    private const double RootDepthToleranceM = 1e-12;
    private const double RootIntervalToleranceN = 1e-11;

    public static void Validate()
    {
        ValidatePointLoadSourceValues();
        ValidateGroupedAndSequentialJumpEquivalence();
        ValidatePiecewiseExactRoot();
        ValidateMidpointConvergence();
        ValidatePointPositionMatters();
    }

    private static void ValidatePointLoadSourceValues()
    {
        AssertNear(7.6875, PointDragN, 1e-12, "combined point drag");
        AssertNear(39.1575, ConnectorWeightWaterKg + PayloadWeightWaterKg, 1e-12, "combined point water weight kg");
        AssertNear(384.003897375, PointWeightWaterN, 1e-9, "combined point water weight force");
    }

    private static void ValidateGroupedAndSequentialJumpEquivalence()
    {
        const double pointS = 30.0;
        const double q0N = 700.0;

        var hBefore = H0N + QxNPerM * pointS;
        var vBefore = q0N - WeightNPerM * pointS;

        var groupedH = hBefore + PointDragN;
        var groupedV = vBefore - PointWeightWaterN;

        var sequentialH = hBefore;
        var sequentialV = vBefore;
        sequentialH += ConnectorDragN;
        sequentialV -= ConnectorWeightWaterKg * G;
        sequentialH += PayloadDragN;
        sequentialV -= PayloadWeightWaterKg * G;

        AssertNear(groupedH, sequentialH, 1e-12, "co-located grouped/sequential H");
        AssertNear(groupedV, sequentialV, 1e-10, "co-located grouped/sequential V");

        // A zero-length jump changes force state only. The geometrical point itself is continuous.
        var upper = ExactInterval(H0N, q0N, pointS);
        RequireAvailable(upper, "upper interval before grouped/sequential jump");
        var xAtPointBefore = upper.XM;
        var zAtPointBefore = upper.ZM;
        var xAtPointAfter = xAtPointBefore;
        var zAtPointAfter = zAtPointBefore;
        AssertNear(xAtPointBefore, xAtPointAfter, 0, "point-jump X continuity");
        AssertNear(zAtPointBefore, zAtPointAfter, 0, "point-jump Z continuity");
    }

    private static void ValidatePiecewiseExactRoot()
    {
        var solved = SolveExact(pointS: 30.0);
        RequireAvailable(solved.Displacement, "piecewise exact solved state");

        AssertNear(ExpectedExactQ0N, solved.Q0N, 2e-8, "piecewise exact Q0");
        AssertNear(ExpectedExactXM, solved.Displacement.XM, 2e-9, "piecewise exact X");
        AssertNear(TargetDepthM, solved.Displacement.ZM, 2e-11, "piecewise exact Z");

        if (solved.Q0N <= 0 || solved.Q0N >= QCapacityN)
        {
            throw new InvalidOperationException(
                $"point-load analytical reference: solved Q0 must lie inside capacity bounds, got {solved.Q0N:R} N.");
        }
    }

    private static void ValidateMidpointConvergence()
    {
        var exact = SolveExact(pointS: 30.0);
        var coarse = SolveMidpoint(pointS: 30.0, nominalDsM: 0.4);
        var productionTarget = SolveMidpoint(pointS: 30.0, nominalDsM: 0.2);
        var fine = SolveMidpoint(pointS: 30.0, nominalDsM: 0.1);
        var finer = SolveMidpoint(pointS: 30.0, nominalDsM: 0.05);

        var qErrors = new[]
        {
            Math.Abs(coarse.Q0N - exact.Q0N),
            Math.Abs(productionTarget.Q0N - exact.Q0N),
            Math.Abs(fine.Q0N - exact.Q0N),
            Math.Abs(finer.Q0N - exact.Q0N)
        };
        var xErrors = new[]
        {
            Math.Abs(coarse.Displacement.XM - exact.Displacement.XM),
            Math.Abs(productionTarget.Displacement.XM - exact.Displacement.XM),
            Math.Abs(fine.Displacement.XM - exact.Displacement.XM),
            Math.Abs(finer.Displacement.XM - exact.Displacement.XM)
        };

        AssertStrictlyDecreasing(qErrors, "point-load Q0 midpoint errors");
        AssertStrictlyDecreasing(xErrors, "point-load X midpoint errors");

        AssertRatioAtLeast(qErrors[0], qErrors[1], 3.5, "point-load Q0 0.4 -> 0.2 ratio");
        AssertRatioAtLeast(qErrors[1], qErrors[2], 3.5, "point-load Q0 0.2 -> 0.1 ratio");
        AssertRatioAtLeast(xErrors[0], xErrors[1], 3.5, "point-load X 0.4 -> 0.2 ratio");
        AssertRatioAtLeast(xErrors[1], xErrors[2], 3.5, "point-load X 0.2 -> 0.1 ratio");

        if (qErrors[1] > 1.5e-4)
        {
            throw new InvalidOperationException(
                $"point-load analytical reference: 0.20 m midpoint Q0 error too large: {qErrors[1]:R} N.");
        }

        if (xErrors[1] > 8e-6)
        {
            throw new InvalidOperationException(
                $"point-load analytical reference: 0.20 m midpoint X error too large: {xErrors[1]:R} m.");
        }

        AssertNear(TargetDepthM, productionTarget.Displacement.ZM, 2e-10, "point-load 0.20 m tight root Z");
    }

    private static void ValidatePointPositionMatters()
    {
        var at20 = SolveExact(20.0);
        var at30 = SolveExact(30.0);
        var at40 = SolveExact(40.0);

        RequireAvailable(at20.Displacement, "point at 20 m");
        RequireAvailable(at30.Displacement, "point at 30 m");
        RequireAvailable(at40.Displacement, "point at 40 m");

        AssertNear(774.773285663587, at20.Q0N, 3e-8, "point at 20 m Q0");
        AssertNear(741.2658404389103, at30.Q0N, 3e-8, "point at 30 m Q0");
        AssertNear(683.1467592562775, at40.Q0N, 3e-8, "point at 40 m Q0");

        AssertNear(20.364669098312767, at20.Displacement.XM, 3e-9, "point at 20 m X");
        AssertNear(19.48029920258567, at30.Displacement.XM, 3e-9, "point at 30 m X");
        AssertNear(18.675354522010974, at40.Displacement.XM, 3e-9, "point at 40 m X");

        if (!(at20.Q0N > at30.Q0N && at30.Q0N > at40.Q0N))
        {
            throw new InvalidOperationException(
                "point-load analytical reference: moving the same point load downward must change the solved Q0 for this deterministic case.");
        }

        if (Math.Abs(at20.Displacement.XM - at40.Displacement.XM) < 1.0)
        {
            throw new InvalidOperationException(
                "point-load analytical reference: point-load position must materially affect X for this deterministic case.");
        }
    }

    private static RootSolution SolveExact(double pointS)
    {
        return SolveRoot(
            q0N => PiecewiseExact(q0N, pointS),
            0,
            QCapacityN);
    }

    private static RootSolution SolveMidpoint(double pointS, double nominalDsM)
    {
        return SolveRoot(
            q0N => PiecewiseMidpoint(q0N, pointS, nominalDsM),
            0,
            QCapacityN);
    }

    private static RootSolution SolveRoot(
        Func<double, DisplacementState> evaluate,
        double lowQ0N,
        double highQ0N)
    {
        var low = evaluate(lowQ0N);
        var high = evaluate(highQ0N);
        RequireAvailable(low, "root low bound");
        RequireAvailable(high, "root high bound");

        var lowResidual = low.ZM - TargetDepthM;
        var highResidual = high.ZM - TargetDepthM;
        if (lowResidual * highResidual >= 0)
        {
            throw new InvalidOperationException(
                $"point-load analytical reference: root not bracketed, residuals {lowResidual:R}, {highResidual:R}.");
        }

        for (var iteration = 0; iteration < 120; iteration++)
        {
            var midQ = (lowQ0N + highQ0N) / 2.0;
            var mid = evaluate(midQ);
            RequireAvailable(mid, "root midpoint");
            var residual = mid.ZM - TargetDepthM;

            if (Math.Abs(residual) <= RootDepthToleranceM ||
                Math.Abs(highQ0N - lowQ0N) <= RootIntervalToleranceN)
            {
                return new RootSolution(midQ, mid);
            }

            if (lowResidual * residual <= 0)
            {
                highQ0N = midQ;
                highResidual = residual;
            }
            else
            {
                lowQ0N = midQ;
                lowResidual = residual;
            }
        }

        var q = (lowQ0N + highQ0N) / 2.0;
        var final = evaluate(q);
        RequireAvailable(final, "root final state");
        return new RootSolution(q, final);
    }

    private static DisplacementState PiecewiseExact(double q0N, double pointS)
    {
        if (pointS <= 0 || pointS >= LengthM)
        {
            throw new ArgumentOutOfRangeException(nameof(pointS));
        }

        var upper = ExactInterval(H0N, q0N, pointS);
        if (!upper.Available)
        {
            return upper;
        }

        var hAfterUpper = H0N + QxNPerM * pointS;
        var vAfterUpper = q0N - WeightNPerM * pointS;
        var hAfterPoint = hAfterUpper + PointDragN;
        var vAfterPoint = vAfterUpper - PointWeightWaterN;

        var lower = ExactInterval(
            hAfterPoint,
            vAfterPoint,
            LengthM - pointS);
        if (!lower.Available)
        {
            return lower;
        }

        return new DisplacementState(
            true,
            upper.XM + lower.XM,
            upper.ZM + lower.ZM,
            "Available");
    }

    private static DisplacementState PiecewiseMidpoint(
        double q0N,
        double pointS,
        double nominalDsM)
    {
        var upper = MidpointInterval(
            H0N,
            q0N,
            pointS,
            nominalDsM);
        if (!upper.Displacement.Available)
        {
            return upper.Displacement;
        }

        var hAfterPoint = upper.TerminalHN + PointDragN;
        var vAfterPoint = upper.TerminalVN - PointWeightWaterN;

        var lower = MidpointInterval(
            hAfterPoint,
            vAfterPoint,
            LengthM - pointS,
            nominalDsM);
        if (!lower.Displacement.Available)
        {
            return lower.Displacement;
        }

        return new DisplacementState(
            true,
            upper.Displacement.XM + lower.Displacement.XM,
            upper.Displacement.ZM + lower.Displacement.ZM,
            "Available");
    }

    private static IntervalState MidpointInterval(
        double hStartN,
        double vStartN,
        double lengthM,
        double nominalDsM)
    {
        var segmentCount = Math.Max(1, (int)Math.Round(lengthM / nominalDsM));
        var ds = lengthM / segmentCount;
        var h = hStartN;
        var v = vStartN;
        var x = 0.0;
        var z = 0.0;

        for (var i = 0; i < segmentCount; i++)
        {
            var hMid = h + 0.5 * QxNPerM * ds;
            var vMid = v - 0.5 * WeightNPerM * ds;
            var tension = Math.Sqrt(hMid * hMid + vMid * vMid);
            if (!double.IsFinite(tension) || tension <= ForceEpsilonN)
            {
                return new IntervalState(
                    new DisplacementState(false, 0, 0, "IndeterminateMidpointResultant"),
                    h,
                    v);
            }

            x += ds * hMid / tension;
            z += ds * vMid / tension;
            h += QxNPerM * ds;
            v -= WeightNPerM * ds;
        }

        return new IntervalState(
            new DisplacementState(true, x, z, "Available"),
            h,
            v);
    }

    private static DisplacementState ExactInterval(
        double hStartN,
        double vStartN,
        double lengthM)
    {
        var qzNPerM = -WeightNPerM;
        var qNorm = Math.Sqrt(QxNPerM * QxNPerM + qzNPerM * qzNPerM);
        var r0Norm = Math.Sqrt(hStartN * hStartN + vStartN * vStartN);

        if (qNorm <= ForceEpsilonN)
        {
            if (r0Norm <= ForceEpsilonN)
            {
                return new DisplacementState(false, 0, 0, "IndeterminateZeroResultant");
            }

            return new DisplacementState(
                true,
                lengthM * hStartN / r0Norm,
                lengthM * vStartN / r0Norm,
                "Available");
        }

        var ex = QxNPerM / qNorm;
        var ez = qzNPerM / qNorm;
        var nx = -qzNPerM / qNorm;
        var nz = QxNPerM / qNorm;

        var u0 = ex * hStartN + ez * vStartN;
        var u1 = u0 + qNorm * lengthM;
        var c = nx * hStartN + nz * vStartN;

        if (Math.Abs(c) <= ForceEpsilonN)
        {
            if (u0 == 0 || u1 == 0 || u0 * u1 < 0)
            {
                return new DisplacementState(false, 0, 0, "IndeterminateCollinearZeroCrossing");
            }

            var sign = Math.Sign(u0);
            return new DisplacementState(
                true,
                ex * sign * lengthM,
                ez * sign * lengthM,
                "Available");
        }

        var r0 = Math.Sqrt(u0 * u0 + c * c);
        var r1 = Math.Sqrt(u1 * u1 + c * c);
        var asinhDelta =
            Math.Asinh(u1 / Math.Abs(c)) -
            Math.Asinh(u0 / Math.Abs(c));

        var x =
            ex * (r1 - r0) / qNorm +
            nx * (c / qNorm) * asinhDelta;
        var z =
            ez * (r1 - r0) / qNorm +
            nz * (c / qNorm) * asinhDelta;

        if (!double.IsFinite(x) || !double.IsFinite(z))
        {
            return new DisplacementState(false, 0, 0, "IndeterminateNonFiniteIntegral");
        }

        return new DisplacementState(true, x, z, "Available");
    }

    private static void RequireAvailable(DisplacementState state, string label)
    {
        if (!state.Available)
        {
            throw new InvalidOperationException(
                $"point-load analytical reference {label}: expected available, got {state.State}.");
        }
    }

    private static void AssertStrictlyDecreasing(IReadOnlyList<double> values, string label)
    {
        for (var i = 1; i < values.Count; i++)
        {
            if (!(values[i] < values[i - 1]))
            {
                throw new InvalidOperationException(
                    $"point-load analytical reference {label}: error did not decrease at index {i}: {values[i - 1]:R} -> {values[i]:R}.");
            }
        }
    }

    private static void AssertRatioAtLeast(double coarseError, double fineError, double minimumRatio, string label)
    {
        if (fineError <= 0)
        {
            return;
        }

        var ratio = coarseError / fineError;
        if (ratio < minimumRatio)
        {
            throw new InvalidOperationException(
                $"point-load analytical reference {label}: expected ratio >= {minimumRatio:R}, got {ratio:R}.");
        }
    }

    private static void AssertNear(double expected, double actual, double tolerance, string label)
    {
        if (!double.IsFinite(expected) || !double.IsFinite(actual) || Math.Abs(expected - actual) > tolerance)
        {
            throw new InvalidOperationException(
                $"point-load analytical reference {label}: expected {expected:R}, got {actual:R}, tolerance {tolerance:R}.");
        }
    }

    private sealed record DisplacementState(bool Available, double XM, double ZM, string State);

    private sealed record IntervalState(
        DisplacementState Displacement,
        double TerminalHN,
        double TerminalVN);

    private sealed record RootSolution(double Q0N, DisplacementState Displacement);
}
