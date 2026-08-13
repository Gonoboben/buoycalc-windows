internal static class ConstantLoadAnalyticalReferenceRegression
{
    private const double LengthM = 55.0;
    private const double TargetDepthM = 50.0;
    private const double H0N = 82.0;
    private const double QxNPerM = 3.075;
    private const double WeightNPerM = 0.980665;
    private const double QCapacityN = 9071.15125;

    private const double ExpectedExactQ0N = 405.4394635275295;
    private const double ExpectedExactXM = 21.911468597524454;

    private const double ForceEpsilonN = 1e-12;
    private const double RootDepthToleranceM = 1e-12;
    private const double RootIntervalToleranceN = 1e-11;

    public static void Validate()
    {
        ValidateExactIntegralAgainstAdaptiveQuadrature();
        ValidateExactSurfaceBoundaryRoot();
        ValidateMidpointMeshConvergence();
        ValidateExactTautLimit();
        ValidateDegenerateStates();
    }

    private static void ValidateExactIntegralAgainstAdaptiveQuadrature()
    {
        var exact = ExactDisplacement(H0N, ExpectedExactQ0N, QxNPerM, WeightNPerM, LengthM);
        RequireAvailable(exact, "exact reference state");

        var quadX = AdaptiveSimpson(
            s => UnitTangentComponent(H0N, ExpectedExactQ0N, QxNPerM, WeightNPerM, s, horizontal: true),
            0,
            LengthM,
            1e-12,
            24);
        var quadZ = AdaptiveSimpson(
            s => UnitTangentComponent(H0N, ExpectedExactQ0N, QxNPerM, WeightNPerM, s, horizontal: false),
            0,
            LengthM,
            1e-12,
            24);

        AssertNear(exact.XM, quadX, 2e-10, "exact X vs adaptive quadrature");
        AssertNear(exact.ZM, quadZ, 2e-10, "exact Z vs adaptive quadrature");
    }

    private static void ValidateExactSurfaceBoundaryRoot()
    {
        var solved = SolveExactQ0(
            H0N,
            QxNPerM,
            WeightNPerM,
            LengthM,
            TargetDepthM,
            0,
            QCapacityN);

        RequireAvailable(solved.Displacement, "exact solved displacement");

        AssertNear(ExpectedExactQ0N, solved.Q0N, 2e-8, "exact Q0 reference");
        AssertNear(ExpectedExactXM, solved.Displacement.XM, 2e-9, "exact X reference");
        AssertNear(TargetDepthM, solved.Displacement.ZM, 2e-11, "exact Z closure");

        if (solved.Q0N <= 0 || solved.Q0N >= QCapacityN)
        {
            throw new InvalidOperationException(
                $"constant-load analytical reference: exact Q0 must be strictly inside capacity bounds, got {solved.Q0N:R} N.");
        }
    }

    private static void ValidateMidpointMeshConvergence()
    {
        var exact = SolveExactQ0(
            H0N,
            QxNPerM,
            WeightNPerM,
            LengthM,
            TargetDepthM,
            0,
            QCapacityN);

        var coarse = SolveMidpointQ0(69);
        var medium = SolveMidpointQ0(138);
        var productionTarget = SolveMidpointQ0(275); // exactly 0.20 m for L=55 m
        var fine = SolveMidpointQ0(550);
        var finer = SolveMidpointQ0(1100);

        var qErrors = new[]
        {
            Math.Abs(coarse.Q0N - exact.Q0N),
            Math.Abs(medium.Q0N - exact.Q0N),
            Math.Abs(productionTarget.Q0N - exact.Q0N),
            Math.Abs(fine.Q0N - exact.Q0N),
            Math.Abs(finer.Q0N - exact.Q0N)
        };

        var xErrors = new[]
        {
            Math.Abs(coarse.Displacement.XM - exact.Displacement.XM),
            Math.Abs(medium.Displacement.XM - exact.Displacement.XM),
            Math.Abs(productionTarget.Displacement.XM - exact.Displacement.XM),
            Math.Abs(fine.Displacement.XM - exact.Displacement.XM),
            Math.Abs(finer.Displacement.XM - exact.Displacement.XM)
        };

        AssertStrictlyDecreasing(qErrors, "midpoint Q0 mesh errors");
        AssertStrictlyDecreasing(xErrors, "midpoint X mesh errors");

        // Midpoint integration is second order for this smooth case.
        AssertRatioAtLeast(qErrors[1], qErrors[2], 3.5, "Q0 medium -> 0.20 m convergence ratio");
        AssertRatioAtLeast(qErrors[2], qErrors[3], 3.5, "Q0 0.20 -> 0.10 m convergence ratio");
        AssertRatioAtLeast(xErrors[1], xErrors[2], 3.5, "X medium -> 0.20 m convergence ratio");
        AssertRatioAtLeast(xErrors[2], xErrors[3], 3.5, "X 0.20 -> 0.10 m convergence ratio");

        if (qErrors[2] > 3e-4)
        {
            throw new InvalidOperationException(
                $"constant-load analytical reference: 0.20 m midpoint Q0 error too large for reference case: {qErrors[2]:R} N.");
        }

        if (xErrors[2] > 1.5e-5)
        {
            throw new InvalidOperationException(
                $"constant-load analytical reference: 0.20 m midpoint X error too large for reference case: {xErrors[2]:R} m.");
        }

        AssertNear(TargetDepthM, productionTarget.Displacement.ZM, 2e-10, "0.20 m midpoint tight root depth");
    }

    private static void ValidateExactTautLimit()
    {
        const double tautLengthM = 50.0;
        const double tautDepthM = 50.0;
        const double tautH0N = 82.0;
        const double tautQxNPerM = 3.075;
        const double tautWeightNPerM = 0.980665;

        var finiteCapacity = ExactDisplacement(
            tautH0N,
            QCapacityN,
            tautQxNPerM,
            tautWeightNPerM,
            tautLengthM);
        RequireAvailable(finiteCapacity, "taut finite-capacity state");

        if (!(finiteCapacity.ZM < tautDepthM))
        {
            throw new InvalidOperationException(
                $"constant-load analytical reference: finite nonzero-H taut state must satisfy Z<L, got Z={finiteCapacity.ZM:R}, L={tautLengthM:R}.");
        }

        var residualM = tautDepthM - finiteCapacity.ZM;
        if (!(residualM > 0 && residualM < 0.01))
        {
            throw new InvalidOperationException(
                $"constant-load analytical reference: taut test should demonstrate a positive exact residual smaller than historical 0.01 m tolerance, got {residualM:R} m.");
        }

        // Mathematical existence test: H(s) remains strictly positive on the whole interval,
        // so |dz/ds| < 1 everywhere at finite tension and exact Z=L is impossible.
        var hAtTop = tautH0N;
        var hAtBottom = tautH0N + tautQxNPerM * tautLengthM;
        if (hAtTop <= 0 || hAtBottom <= 0)
        {
            throw new InvalidOperationException("constant-load analytical reference: taut proof requires strictly positive H throughout the synthetic interval.");
        }
    }

    private static void ValidateDegenerateStates()
    {
        var zero = ExactDisplacement(0, 0, 0, 0, 10);
        if (zero.Available || zero.State != "IndeterminateZeroResultant")
        {
            throw new InvalidOperationException(
                $"constant-load analytical reference: zero resultant must be indeterminate, got {zero.State}.");
        }

        // r(s)=(-0.5+s, 0) crosses exactly through zero inside the interval.
        var crossing = ExactDisplacement(-0.5, 0, 1.0, 0, 1.0);
        if (crossing.Available || crossing.State != "IndeterminateCollinearZeroCrossing")
        {
            throw new InvalidOperationException(
                $"constant-load analytical reference: collinear zero crossing must be indeterminate, got {crossing.State}.");
        }

        var straight = ExactDisplacement(1.0, 0, 1.0, 0, 2.0);
        RequireAvailable(straight, "collinear non-crossing state");
        AssertNear(2.0, straight.XM, 1e-12, "collinear straight X");
        AssertNear(0.0, straight.ZM, 1e-12, "collinear straight Z");
    }

    private static RootSolution SolveExactQ0(
        double h0N,
        double qxNPerM,
        double weightNPerM,
        double lengthM,
        double targetDepthM,
        double lowQ0N,
        double highQ0N)
    {
        var low = ExactDisplacement(h0N, lowQ0N, qxNPerM, weightNPerM, lengthM);
        var high = ExactDisplacement(h0N, highQ0N, qxNPerM, weightNPerM, lengthM);
        RequireAvailable(low, "exact root low bound");
        RequireAvailable(high, "exact root high bound");

        var lowResidual = low.ZM - targetDepthM;
        var highResidual = high.ZM - targetDepthM;
        if (lowResidual * highResidual >= 0)
        {
            throw new InvalidOperationException(
                $"constant-load analytical reference: exact root must be bracketed, residuals {lowResidual:R}, {highResidual:R}.");
        }

        for (var iteration = 0; iteration < 120; iteration++)
        {
            var midQ = (lowQ0N + highQ0N) / 2.0;
            var mid = ExactDisplacement(h0N, midQ, qxNPerM, weightNPerM, lengthM);
            RequireAvailable(mid, "exact root midpoint");
            var residual = mid.ZM - targetDepthM;

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
        var displacement = ExactDisplacement(h0N, q, qxNPerM, weightNPerM, lengthM);
        RequireAvailable(displacement, "exact root final state");
        return new RootSolution(q, displacement);
    }

    private static RootSolution SolveMidpointQ0(int segmentCount)
    {
        if (segmentCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(segmentCount));
        }

        var lowQ = 0.0;
        var highQ = QCapacityN;
        var low = MidpointDisplacement(lowQ, segmentCount);
        var high = MidpointDisplacement(highQ, segmentCount);
        var lowResidual = low.ZM - TargetDepthM;
        var highResidual = high.ZM - TargetDepthM;

        if (lowResidual * highResidual >= 0)
        {
            throw new InvalidOperationException(
                $"constant-load analytical reference: midpoint root must be bracketed for N={segmentCount}.");
        }

        for (var iteration = 0; iteration < 120; iteration++)
        {
            var midQ = (lowQ + highQ) / 2.0;
            var mid = MidpointDisplacement(midQ, segmentCount);
            var residual = mid.ZM - TargetDepthM;

            if (Math.Abs(residual) <= RootDepthToleranceM ||
                Math.Abs(highQ - lowQ) <= RootIntervalToleranceN)
            {
                return new RootSolution(midQ, mid);
            }

            if (lowResidual * residual <= 0)
            {
                highQ = midQ;
                highResidual = residual;
            }
            else
            {
                lowQ = midQ;
                lowResidual = residual;
            }
        }

        var q = (lowQ + highQ) / 2.0;
        return new RootSolution(q, MidpointDisplacement(q, segmentCount));
    }

    private static DisplacementState MidpointDisplacement(double q0N, int segmentCount)
    {
        var ds = LengthM / segmentCount;
        var x = 0.0;
        var z = 0.0;
        var h = H0N;
        var v = q0N;

        for (var i = 0; i < segmentCount; i++)
        {
            var hMid = h + 0.5 * QxNPerM * ds;
            var vMid = v - 0.5 * WeightNPerM * ds;
            var tension = Math.Sqrt(hMid * hMid + vMid * vMid);
            if (!double.IsFinite(tension) || tension <= ForceEpsilonN)
            {
                return new DisplacementState(false, 0, 0, "IndeterminateMidpointResultant");
            }

            x += ds * hMid / tension;
            z += ds * vMid / tension;
            h += QxNPerM * ds;
            v -= WeightNPerM * ds;
        }

        return new DisplacementState(true, x, z, "Available");
    }

    private static DisplacementState ExactDisplacement(
        double h0N,
        double q0N,
        double qxNPerM,
        double weightNPerM,
        double lengthM)
    {
        var qzNPerM = -weightNPerM;
        var qNorm = Math.Sqrt(qxNPerM * qxNPerM + qzNPerM * qzNPerM);
        var r0Norm = Math.Sqrt(h0N * h0N + q0N * q0N);

        if (qNorm <= ForceEpsilonN)
        {
            if (r0Norm <= ForceEpsilonN)
            {
                return new DisplacementState(false, 0, 0, "IndeterminateZeroResultant");
            }

            return new DisplacementState(
                true,
                lengthM * h0N / r0Norm,
                lengthM * q0N / r0Norm,
                "Available");
        }

        var ex = qxNPerM / qNorm;
        var ez = qzNPerM / qNorm;
        var nx = -qzNPerM / qNorm;
        var nz = qxNPerM / qNorm;

        var u0 = ex * h0N + ez * q0N;
        var u1 = u0 + qNorm * lengthM;
        var c = nx * h0N + nz * q0N;

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

    private static double UnitTangentComponent(
        double h0N,
        double q0N,
        double qxNPerM,
        double weightNPerM,
        double s,
        bool horizontal)
    {
        var h = h0N + qxNPerM * s;
        var v = q0N - weightNPerM * s;
        var tension = Math.Sqrt(h * h + v * v);
        if (!double.IsFinite(tension) || tension <= ForceEpsilonN)
        {
            throw new InvalidOperationException(
                $"constant-load analytical reference: adaptive quadrature encountered degenerate resultant at s={s:R}.");
        }

        return horizontal ? h / tension : v / tension;
    }

    private static double AdaptiveSimpson(
        Func<double, double> function,
        double a,
        double b,
        double tolerance,
        int maxDepth)
    {
        var fa = function(a);
        var fb = function(b);
        var mid = (a + b) / 2.0;
        var fm = function(mid);
        var whole = Simpson(a, b, fa, fm, fb);
        return AdaptiveSimpsonRecursive(function, a, b, fa, fm, fb, whole, tolerance, maxDepth);
    }

    private static double AdaptiveSimpsonRecursive(
        Func<double, double> function,
        double a,
        double b,
        double fa,
        double fm,
        double fb,
        double whole,
        double tolerance,
        int depth)
    {
        var mid = (a + b) / 2.0;
        var leftMid = (a + mid) / 2.0;
        var rightMid = (mid + b) / 2.0;
        var fLeftMid = function(leftMid);
        var fRightMid = function(rightMid);
        var left = Simpson(a, mid, fa, fLeftMid, fm);
        var right = Simpson(mid, b, fm, fRightMid, fb);
        var delta = left + right - whole;

        if (depth <= 0 || Math.Abs(delta) <= 15.0 * tolerance)
        {
            return left + right + delta / 15.0;
        }

        return AdaptiveSimpsonRecursive(
                   function,
                   a,
                   mid,
                   fa,
                   fLeftMid,
                   fm,
                   left,
                   tolerance / 2.0,
                   depth - 1) +
               AdaptiveSimpsonRecursive(
                   function,
                   mid,
                   b,
                   fm,
                   fRightMid,
                   fb,
                   right,
                   tolerance / 2.0,
                   depth - 1);
    }

    private static double Simpson(
        double a,
        double b,
        double fa,
        double fm,
        double fb)
    {
        return (b - a) * (fa + 4.0 * fm + fb) / 6.0;
    }

    private static void RequireAvailable(DisplacementState state, string label)
    {
        if (!state.Available)
        {
            throw new InvalidOperationException(
                $"constant-load analytical reference {label}: expected available state, got {state.State}.");
        }
    }

    private static void AssertStrictlyDecreasing(IReadOnlyList<double> values, string label)
    {
        for (var i = 1; i < values.Count; i++)
        {
            if (!(values[i] < values[i - 1]))
            {
                throw new InvalidOperationException(
                    $"constant-load analytical reference {label}: error did not decrease at index {i}: {values[i - 1]:R} -> {values[i]:R}.");
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
                $"constant-load analytical reference {label}: expected ratio >= {minimumRatio:R}, got {ratio:R}.");
        }
    }

    private static void AssertNear(double expected, double actual, double tolerance, string label)
    {
        if (!double.IsFinite(expected) || !double.IsFinite(actual) || Math.Abs(expected - actual) > tolerance)
        {
            throw new InvalidOperationException(
                $"constant-load analytical reference {label}: expected {expected:R}, got {actual:R}, tolerance {tolerance:R}.");
        }
    }

    private sealed record DisplacementState(bool Available, double XM, double ZM, string State);

    private sealed record RootSolution(double Q0N, DisplacementState Displacement);
}
