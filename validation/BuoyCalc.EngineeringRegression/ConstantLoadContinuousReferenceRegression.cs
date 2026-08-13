internal static class ConstantLoadContinuousReferenceRegression
{
    private const double G = 9.80665;
    private const double LineLengthM = 55.0;
    private const double TargetDepthM = 50.0;
    private const double H0N = 82.0;
    private const double DistributedHorizontalNPerM = 3.075;
    private const double DistributedVerticalDerivativeNPerM = -0.1 * G;
    private const double QCapacityN = (1025.0 - 100.0) * G;
    private const double VectorEpsilon = 1e-12;
    private const double RootToleranceM = 1e-11;
    private const int MaxRootIterations = 160;

    public static void Validate()
    {
        ValidateClosedFormSanity();

        var exact = SolveBoundedRoot(q0 => ExactDisplacement(q0));
        AssertNear(TargetDepthM, exact.ZM, 1e-9, "exact continuous root depth");

        if (exact.Q0N <= 0 || exact.Q0N >= QCapacityN)
        {
            throw new InvalidOperationException(
                $"constant-load reference: exact Q0={exact.Q0N:R} N must lie strictly inside the buoyancy-capacity bracket.");
        }

        var stepSizes = new[] { 1.0, 0.5, 0.2, 0.1 };
        var start = stepSizes.ToDictionary(
            step => step,
            step => SolveBoundedRoot(q0 => DiscreteDisplacement(q0, step, QuadratureRule.StartNode)));
        var midpoint = stepSizes.ToDictionary(
            step => step,
            step => SolveBoundedRoot(q0 => DiscreteDisplacement(q0, step, QuadratureRule.Midpoint)));

        ValidateMonotoneRefinement("start-node Q0", stepSizes, start, x => Math.Abs(x.Q0N - exact.Q0N));
        ValidateMonotoneRefinement("start-node X", stepSizes, start, x => Math.Abs(x.XM - exact.XM));
        ValidateMonotoneRefinement("midpoint Q0", stepSizes, midpoint, x => Math.Abs(x.Q0N - exact.Q0N));
        ValidateMonotoneRefinement("midpoint X", stepSizes, midpoint, x => Math.Abs(x.XM - exact.XM));

        AssertObservedOrder(
            "start-node Q0 1.0->0.5",
            1.0,
            Math.Abs(start[1.0].Q0N - exact.Q0N),
            0.5,
            Math.Abs(start[0.5].Q0N - exact.Q0N),
            0.8,
            1.2);
        AssertObservedOrder(
            "start-node Q0 0.2->0.1",
            0.2,
            Math.Abs(start[0.2].Q0N - exact.Q0N),
            0.1,
            Math.Abs(start[0.1].Q0N - exact.Q0N),
            0.8,
            1.2);
        AssertObservedOrder(
            "midpoint Q0 1.0->0.5",
            1.0,
            Math.Abs(midpoint[1.0].Q0N - exact.Q0N),
            0.5,
            Math.Abs(midpoint[0.5].Q0N - exact.Q0N),
            1.8,
            2.2);
        AssertObservedOrder(
            "midpoint Q0 0.2->0.1",
            0.2,
            Math.Abs(midpoint[0.2].Q0N - exact.Q0N),
            0.1,
            Math.Abs(midpoint[0.1].Q0N - exact.Q0N),
            1.8,
            2.2);

        var startProductionStepQError = Math.Abs(start[0.2].Q0N - exact.Q0N);
        var midpointProductionStepQError = Math.Abs(midpoint[0.2].Q0N - exact.Q0N);
        var startProductionStepXError = Math.Abs(start[0.2].XM - exact.XM);
        var midpointProductionStepXError = Math.Abs(midpoint[0.2].XM - exact.XM);

        if (!(midpointProductionStepQError < 0.01 * startProductionStepQError))
        {
            throw new InvalidOperationException(
                $"constant-load reference: at 0.20 m midpoint Q0 error {midpointProductionStepQError:R} N must be materially smaller than start-node error {startProductionStepQError:R} N.");
        }

        if (!(midpointProductionStepXError < 0.01 * startProductionStepXError))
        {
            throw new InvalidOperationException(
                $"constant-load reference: at 0.20 m midpoint X error {midpointProductionStepXError:R} m must be materially smaller than start-node error {startProductionStepXError:R} m.");
        }

        Console.WriteLine(
            $"CONST_LOAD_REFERENCE exactQ0N={exact.Q0N:R}; exactX={exact.XM:R}; exactZ={exact.ZM:R}; " +
            $"H0={H0N:R}; qH={DistributedHorizontalNPerM:R}; qV={DistributedVerticalDerivativeNPerM:R}; L={LineLengthM:R}; D={TargetDepthM:R}");

        foreach (var step in stepSizes)
        {
            PrintEvidence("start", step, start[step], exact);
            PrintEvidence("midpoint", step, midpoint[step], exact);
        }
    }

    private static void ValidateClosedFormSanity()
    {
        var h = 3.0;
        var v = 4.0;
        var length = 7.0;
        var displacement = ExactDisplacement(
            h,
            v,
            qHNPerM: 0,
            qVNPerM: 0,
            lengthM: length);

        AssertNear(length * 3.0 / 5.0, displacement.XM, 1e-12, "zero-load exact X");
        AssertNear(length * 4.0 / 5.0, displacement.ZM, 1e-12, "zero-load exact Z");
    }

    private static Point ExactDisplacement(double q0N)
    {
        return ExactDisplacement(
            H0N,
            q0N,
            DistributedHorizontalNPerM,
            DistributedVerticalDerivativeNPerM,
            LineLengthM);
    }

    private static Point ExactDisplacement(
        double h0N,
        double v0N,
        double qHNPerM,
        double qVNPerM,
        double lengthM)
    {
        var initialMagnitude = Math.Sqrt(h0N * h0N + v0N * v0N);
        var qMagnitude = Math.Sqrt(qHNPerM * qHNPerM + qVNPerM * qVNPerM);

        if (qMagnitude <= VectorEpsilon)
        {
            if (initialMagnitude <= VectorEpsilon)
            {
                throw new InvalidOperationException(
                    "constant-load reference: zero initial tension with zero distributed derivative is indeterminate.");
            }

            return new Point(
                lengthM * h0N / initialMagnitude,
                lengthM * v0N / initialMagnitude);
        }

        var ux = qHNPerM / qMagnitude;
        var uz = qVNPerM / qMagnitude;
        var parallel0 = h0N * ux + v0N * uz;
        var px = h0N - parallel0 * ux;
        var pz = v0N - parallel0 * uz;
        var perpendicularMagnitude = Math.Sqrt(px * px + pz * pz);
        var y0 = parallel0;
        var y1 = parallel0 + qMagnitude * lengthM;

        if (perpendicularMagnitude <= VectorEpsilon)
        {
            if (y0 * y1 <= 0)
            {
                throw new InvalidOperationException(
                    "constant-load reference: collinear tension field crosses zero and has no defined tangent there.");
            }

            var sign = Math.Sign(y0);
            return new Point(sign * lengthM * ux, sign * lengthM * uz);
        }

        var f0 = PrimitiveVector(y0, perpendicularMagnitude, ux, uz, px, pz);
        var f1 = PrimitiveVector(y1, perpendicularMagnitude, ux, uz, px, pz);

        return new Point(
            (f1.XM - f0.XM) / qMagnitude,
            (f1.ZM - f0.ZM) / qMagnitude);
    }

    private static Point PrimitiveVector(
        double y,
        double perpendicularMagnitude,
        double ux,
        double uz,
        double px,
        double pz)
    {
        var magnitude = Math.Sqrt(perpendicularMagnitude * perpendicularMagnitude + y * y);
        var asinh = Math.Asinh(y / perpendicularMagnitude);

        return new Point(
            ux * magnitude + px * asinh,
            uz * magnitude + pz * asinh);
    }

    private static Point DiscreteDisplacement(
        double q0N,
        double requestedStepM,
        QuadratureRule rule)
    {
        var segmentCount = (int)Math.Round(LineLengthM / requestedStepM);
        if (segmentCount <= 0)
        {
            throw new InvalidOperationException("constant-load reference: segment count must be positive.");
        }

        var stepM = LineLengthM / segmentCount;
        AssertNear(requestedStepM, stepM, 1e-12, $"requested step {requestedStepM:R} m");

        var xM = 0.0;
        var zM = 0.0;

        for (var i = 0; i < segmentCount; i++)
        {
            var sampleS = rule == QuadratureRule.Midpoint
                ? (i + 0.5) * stepM
                : i * stepM;
            var hN = H0N + DistributedHorizontalNPerM * sampleS;
            var vN = q0N + DistributedVerticalDerivativeNPerM * sampleS;
            var tensionN = Math.Sqrt(hN * hN + vN * vN);

            if (tensionN <= VectorEpsilon)
            {
                throw new InvalidOperationException(
                    $"constant-load reference: {rule} sample encountered degenerate tension at s={sampleS:R} m.");
            }

            xM += stepM * hN / tensionN;
            zM += stepM * vN / tensionN;
        }

        return new Point(xM, zM);
    }

    private static RootResult SolveBoundedRoot(Func<double, Point> geometry)
    {
        var lowQ = 0.0;
        var highQ = QCapacityN;
        var lowPoint = geometry(lowQ);
        var highPoint = geometry(highQ);
        var lowResidual = lowPoint.ZM - TargetDepthM;
        var highResidual = highPoint.ZM - TargetDepthM;

        if (!HasSignChangingBracket(lowResidual, highResidual))
        {
            throw new InvalidOperationException(
                $"constant-load reference: expected a bounded depth bracket, got residuals {lowResidual:R} and {highResidual:R} m.");
        }

        for (var iteration = 1; iteration <= MaxRootIterations; iteration++)
        {
            var midQ = (lowQ + highQ) / 2.0;
            var midPoint = geometry(midQ);
            var midResidual = midPoint.ZM - TargetDepthM;

            if (Math.Abs(midResidual) <= RootToleranceM)
            {
                return new RootResult(midQ, midPoint.XM, midPoint.ZM, iteration);
            }

            if (HasSignChangingBracket(lowResidual, midResidual))
            {
                highQ = midQ;
                highResidual = midResidual;
            }
            else
            {
                lowQ = midQ;
                lowResidual = midResidual;
            }
        }

        var finalQ = (lowQ + highQ) / 2.0;
        var finalPoint = geometry(finalQ);
        if (Math.Abs(finalPoint.ZM - TargetDepthM) > 10 * RootToleranceM)
        {
            throw new InvalidOperationException(
                $"constant-load reference: bounded root failed to converge, residual={finalPoint.ZM - TargetDepthM:R} m.");
        }

        return new RootResult(finalQ, finalPoint.XM, finalPoint.ZM, MaxRootIterations);
    }

    private static bool HasSignChangingBracket(double a, double b)
    {
        return (a < 0 && b > 0) || (a > 0 && b < 0) || a == 0 || b == 0;
    }

    private static void ValidateMonotoneRefinement(
        string label,
        IReadOnlyList<double> stepSizes,
        IReadOnlyDictionary<double, RootResult> values,
        Func<RootResult, double> error)
    {
        var previous = double.PositiveInfinity;
        foreach (var step in stepSizes)
        {
            var current = error(values[step]);
            if (!(current < previous))
            {
                throw new InvalidOperationException(
                    $"constant-load reference {label}: refinement error must decrease; step={step:R}, error={current:R}, previous={previous:R}.");
            }
            previous = current;
        }
    }

    private static void AssertObservedOrder(
        string label,
        double coarseStep,
        double coarseError,
        double fineStep,
        double fineError,
        double minOrder,
        double maxOrder)
    {
        if (coarseError <= 0 || fineError <= 0)
        {
            throw new InvalidOperationException(
                $"constant-load reference {label}: convergence-order errors must be positive.");
        }

        var order = Math.Log(coarseError / fineError) / Math.Log(coarseStep / fineStep);
        if (order < minOrder || order > maxOrder)
        {
            throw new InvalidOperationException(
                $"constant-load reference {label}: observed order {order:R} is outside [{minOrder:R}, {maxOrder:R}].");
        }
    }

    private static void PrintEvidence(
        string rule,
        double stepM,
        RootResult approximate,
        RootResult exact)
    {
        Console.WriteLine(
            "CONST_LOAD_QUADRATURE " +
            $"rule={rule}; stepM={stepM:R}; Q0N={approximate.Q0N:R}; X={approximate.XM:R}; Z={approximate.ZM:R}; " +
            $"Q0ErrorN={approximate.Q0N - exact.Q0N:R}; XErrorM={approximate.XM - exact.XM:R}; iterations={approximate.Iterations}");
    }

    private static void AssertNear(double expected, double actual, double tolerance, string label)
    {
        if (!double.IsFinite(expected) || !double.IsFinite(actual) || Math.Abs(expected - actual) > tolerance)
        {
            throw new InvalidOperationException(
                $"constant-load reference {label}: expected {expected:R}, got {actual:R}, tolerance {tolerance:R}.");
        }
    }

    private enum QuadratureRule
    {
        StartNode,
        Midpoint
    }

    private sealed record Point(double XM, double ZM);

    private sealed record RootResult(
        double Q0N,
        double XM,
        double ZM,
        int Iterations);
}
