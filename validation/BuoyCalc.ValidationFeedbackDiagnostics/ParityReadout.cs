using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

// BC-AUD-006-PARITY-READOUT: observational validation mirror only.
// Not a production evaluator observer; does not authorize Accepted or physics changes.
internal sealed record ParityReadoutField(string Stage, int Segment, string Name, string Bits);
internal sealed record ParityStepSample(string Scenario, int Iteration, IReadOnlyList<ParityReadoutField> Fields);

internal sealed class ParityReadoutReport
{
    private const int Budget = MooringSignedCandidateResult.ProductionFeedbackBudget;
    private static readonly int[] TargetSegments = { 105, 150, 166, 243 };
    private readonly List<ParityStepSample> _steps = new();
    private readonly List<ParityStepSample> _production = new();

    private static string Bits(double value) =>
        BitConverter.DoubleToInt64Bits(value).ToString("X16", CultureInfo.InvariantCulture);

    private static bool Same(ParityStepSample a, ParityStepSample b)
    {
        if (a.Scenario != b.Scenario || a.Iteration != b.Iteration ||
            a.Fields.Count != b.Fields.Count) return false;
        for (var i = 0; i < a.Fields.Count; i++)
        {
            var x = a.Fields[i];
            var y = b.Fields[i];
            if (x.Stage != y.Stage || x.Segment != y.Segment ||
                x.Name != y.Name || x.Bits != y.Bits) return false;
        }
        return true;
    }

    public static ParityStepSample Capture(FixedParityStep step)
    {
        var fields = new List<ParityReadoutField>();
        void Record(string stage, int segment, string field, double value) =>
            fields.Add(new ParityReadoutField(stage, segment, field, Bits(value)));

        var sequence = step.Sequence;
        var old = step.Previous;
        var oldLine = old.Result.SegmentRows.OrderBy(s => s.Number).Sum(s => s.CurrentForceN);
        var nextLine = step.NextResult.SegmentRows.OrderBy(s => s.Number).Sum(s => s.CurrentForceN);
        var ordered = sequence.Rows.OrderBy(x => x.Number).ToList();
        var pointSum = ordered
            .Where((x, i) => x.IsDiscrete && i != 0 && i != ordered.Count - 1)
            .Sum(x => x.CurrentForceN);
        var discrete = sequence.DiscreteCurrentForceN;

        Record("Before", 0, "FixedBuoyDragN", step.FixedBuoyDragN);
        Record("Before", 0, "ResultCurrentForceN", old.Result.CurrentForceN);
        Record("Before", 0, "SegmentSumN", oldLine);
        Record("Before", 0, "PointSumN", pointSum);
        Record("Before", 0, "DiscreteCurrentForceN", discrete);
        Record("Before", 0, "RecoveredBuoyDragN", old.Result.CurrentForceN - oldLine - pointSum);
        Record("Before", 0, "BoundaryBuoyDragN", old.Boundary.BuoySteadyDragN ?? double.NaN);
        Record("Before", 0, "TraceInitialHN", old.Trace.Rows[0].StartHN);
        Record("Before", 0, "BoundaryQ0N", old.Boundary.Q0N ?? double.NaN);

        var firstAdd = step.FixedBuoyDragN + nextLine;
        var recomposed = firstAdd + discrete;
        var recovered = step.NextResult.CurrentForceN - nextLine - pointSum;
        var boundaryDrag = step.NextBoundary.BuoySteadyDragN
            ?? throw new InvalidOperationException($"{step.Scenario}: missing next buoy drag.");
        var traceStart = step.NextTrace.Rows[0].StartHN;
        Record("After", 0, "FixedBuoyDragN", step.FixedBuoyDragN);
        Record("After", 0, "UpdatedLineForceN", step.UpdatedLineForceN);
        Record("After", 0, "SegmentSumN", nextLine);
        Record("After", 0, "PointSumN", pointSum);
        Record("After", 0, "DiscreteCurrentForceN", discrete);
        Record("After", 0, "FixedPlusLineN", firstAdd);
        Record("After", 0, "RecomposedTotalN", recomposed);
        Record("After", 0, "ResultCurrentForceN", step.NextResult.CurrentForceN);
        Record("After", 0, "RecoveredBuoyDragN", recovered);
        Record("After", 0, "BoundaryBuoyDragN", boundaryDrag);
        Record("After", 0, "TraceInitialHN", traceStart);
        Record("After", 0, "BoundaryQ0N", step.NextBoundary.Q0N ?? double.NaN);

        // These are exact arithmetic identities for this validation mirror.
        // No tolerance is used to accept or correct any floating-point state.
        if (Bits(step.UpdatedLineForceN) != Bits(nextLine) ||
            Bits(recomposed) != Bits(step.NextResult.CurrentForceN) ||
            Bits(boundaryDrag) != Bits(Math.Max(0.0, recovered)) ||
            Bits(traceStart) != Bits(boundaryDrag))
            throw new InvalidOperationException(
                $"{step.Scenario} iteration {step.Iteration}: failed observed force-recomposition identity.");

        var segments = old.Result.SegmentRows.ToDictionary(s => s.Number);
        var forceRows = step.ShapeForces.Rows.ToDictionary(x => x.SegmentNumber);
        for (var i = 0; i < step.Projection.Rows.Count; i++)
        {
            var projected = step.Projection.Rows[i];
            var segmentNumber = projected.SegmentNumber;
            if (!TargetSegments.Contains(segmentNumber)) continue;
            if (!segments.TryGetValue(segmentNumber, out var segment) ||
                !forceRows.TryGetValue(segmentNumber, out var force))
                throw new InvalidOperationException($"{step.Scenario}: missing target segment {segmentNumber}.");

            var start = old.Geometry.Nodes[i];
            var end = old.Geometry.Nodes[i + 1];
            var ds = projected.SegmentLengthM;
            var dx = projected.DeltaXM;
            var dz = projected.DeltaZM;
            var tx = dx / Math.Max(0.0001, ds);
            var tz = dz / Math.Max(0.0001, ds);
            var ux = segment.LocalSpeedMS;
            var uz = segment.VerticalCurrentMS;
            var speedSquared = ux * ux + uz * uz;
            var speed = Math.Sqrt(speedSquared);
            var dot = ux * tx + uz * tz;
            var normalSquared = Math.Max(0.0, speed * speed - dot * dot);
            var normalSpeed = Math.Sqrt(normalSquared);

            Record("Projection", segmentNumber, "StartX", start.X);
            Record("Projection", segmentNumber, "EndX", end.X);
            Record("Projection", segmentNumber, "StartZ", start.Z);
            Record("Projection", segmentNumber, "EndZ", end.Z);
            Record("Projection", segmentNumber, "SegmentLengthM", ds);
            Record("Projection", segmentNumber, "DeltaX", dx);
            Record("Projection", segmentNumber, "DeltaZ", dz);
            Record("Projection", segmentNumber, "Tx", tx);
            Record("Projection", segmentNumber, "Tz", tz);
            Record("Projection", segmentNumber, "LocalCurrentX", ux);
            Record("Projection", segmentNumber, "LocalCurrentZ", uz);
            Record("Projection", segmentNumber, "SpeedSquared", speedSquared);
            Record("Projection", segmentNumber, "Speed", speed);
            Record("Projection", segmentNumber, "Dot", dot);
            Record("Projection", segmentNumber, "NormalSquared", normalSquared);
            Record("Projection", segmentNumber, "NormalSpeed", normalSpeed);
            Record("Projection", segmentNumber, "ObservedShapeForceN", force.ShapeForceN);
            Record("Projection", segmentNumber, "PreviousSegmentForceN", segment.CurrentForceN);
            Record("Projection", segmentNumber, "NextSegmentForceN",
                step.NextResult.SegmentRows[i].CurrentForceN);
        }

        if (fields.GroupBy(x => (x.Stage, x.Segment, x.Name)).Any(g => g.Count() != 1))
            throw new InvalidOperationException("BC-AUD-006-PARITY: duplicate field identity.");
        return new ParityStepSample(step.Scenario, step.Iteration, fields);
    }

    public static ParityStepSample FromProduction(string scenario, MooringSignedFeedbackObservedStep observed) =>
        new(scenario, observed.Iteration, observed.Fields.Select(field =>
            new ParityReadoutField(field.Stage, field.Segment, field.Name, field.Bits)).ToArray());

    public void Analyze(string scenario, IReadOnlyList<ParityStepSample> first,
        IReadOnlyList<ParityStepSample> replay, IReadOnlyList<ParityStepSample> production)
    {
        if (first.Count != Budget || replay.Count != Budget || production.Count != Budget)
            throw new InvalidOperationException($"{scenario}: incomplete parity-readout step count.");
        for (var i = 0; i < Budget; i++)
        {
            if (first[i].Scenario != scenario || first[i].Iteration != i + 1 ||
                !Same(first[i], replay[i]))
                throw new InvalidOperationException(
                    $"{scenario}: parity-readout replay bit mismatch at iteration {i + 1}.");
            if (!Same(first[i], production[i]))
                throw new InvalidOperationException(
                    $"{scenario}: actual production/mirror stage bits differ at iteration {i + 1}.");
        }
        _steps.AddRange(first);
        _production.AddRange(production);
        Console.WriteLine($"BC_AUD_006_PARITY_READOUT|Scenario={scenario}|Steps={Budget}|ReplayBitsIdentical=True|ProductionStageParityBitIdentical=True|Scope=ObservedFieldsOnly|SolverModified=False");
    }

    public void Write(string output)
    {
        if (_steps.Count != 3 * Budget || _production.Count != 3 * Budget)
            throw new InvalidOperationException("Incomplete mirror/production parity-readout.");
        Directory.CreateDirectory(output);
        using (var raw = new StreamWriter(Path.Combine(output, "parity-readout-fields.csv"), false, new UTF8Encoding(false)))
        using (var manifest = new StreamWriter(Path.Combine(output, "parity-readout-step-sha256.csv"), false, new UTF8Encoding(false)))
        {
            raw.WriteLine("Scenario,Iteration,Stage,SegmentNumber,Field,RawBits");
            manifest.WriteLine("Scenario,Iteration,FieldCount,SHA256");
            foreach (var step in _steps)
            {
                var canonical = new StringBuilder();
                foreach (var field in step.Fields)
                {
                    raw.WriteLine(string.Join(",",
                        step.Scenario,
                        step.Iteration.ToString(CultureInfo.InvariantCulture),
                        field.Stage,
                        field.Segment.ToString(CultureInfo.InvariantCulture),
                        field.Name, field.Bits));
                    canonical.Append(field.Stage).Append("/")
                        .Append(field.Segment.ToString(CultureInfo.InvariantCulture)).Append("/")
                        .Append(field.Name).Append("=").Append(field.Bits).Append('\n');
                }
                var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString())));
                manifest.WriteLine(string.Join(",",
                    step.Scenario,
                    step.Iteration.ToString(CultureInfo.InvariantCulture),
                    step.Fields.Count.ToString(CultureInfo.InvariantCulture), hash));
            }
        }

        using (var differences = new StreamWriter(Path.Combine(output, "parity-readout-orbit-differences.csv"), false, new UTF8Encoding(false)))
        {
            differences.WriteLine("Scenario,Stage,SegmentNumber,Field,State63Bits,State64Bits");
            foreach (var group in _steps.GroupBy(x => x.Scenario))
            {
                var a = group.Single(s => s.Iteration == 63);
                var b = group.Single(s => s.Iteration == 64);
                if (a.Fields.Count != b.Fields.Count)
                    throw new InvalidOperationException($"{a.Scenario}: changing observed stage schema.");
                for (var i = 0; i < a.Fields.Count; i++)
                {
                    var x = a.Fields[i];
                    var y = b.Fields[i];
                    if (x.Stage != y.Stage || x.Segment != y.Segment || x.Name != y.Name)
                        throw new InvalidOperationException($"{a.Scenario}: reordered observed stage field.");
                    if (x.Bits != y.Bits)
                        differences.WriteLine(string.Join(",",
                            a.Scenario, x.Stage, x.Segment.ToString(CultureInfo.InvariantCulture),
                            x.Name, x.Bits, y.Bits));
                }
            }
        }

        WriteProductionEvidence(output);

        File.WriteAllText(Path.Combine(output, "parity-readout-method.md"), string.Join("\n",
            "# BC-AUD-006-PARITY-READOUT",
            "",
            "Source: validation-only two-pass mirror; production has no internal iteration observer.",
            "Every step 1..64 in A06/A11/A15 is recorded from the existing mirror stages.",
            "After.Stage values come from real post-apply/boundary/trace outputs, not solver replacements.",
            "Projection stage records exact geometry endpoints/differences, speed, dot, normal speed and calculated shape-force rows.",
            "Full raw 64-bit fields are in parity-readout-fields.csv.",
            "Recompute per-step SHA256 from UTF-8 lines: Stage/SegmentNumber/Field=RawBits followed by LF, in CSV row order within Scenario/Iteration; no BOM/header.",
            "parity-readout-orbit-differences.csv compares observed stage outputs at iterations 63 and 64, not a production trajectory.",
            "Actual production observer and validation mirror matched by DIRECT raw-bit comparison across all instrumented fields.",
            "This does not prove unobserved internal state equivalence, physical validity, root cause or F1-F4 equivalence.",
            "parity-production-fields.csv / parity-production-step-sha256.csv publish actual production observer evidence.",
            "Do not convert BudgetExhausted to Accepted. BC-AUD-006 remains OPEN.",
            ""), new UTF8Encoding(false));
    }

    private void WriteProductionEvidence(string output)
    {
        using var raw = new StreamWriter(Path.Combine(output, "parity-production-fields.csv"),
            false, new UTF8Encoding(false));
        using var manifest = new StreamWriter(Path.Combine(output, "parity-production-step-sha256.csv"),
            false, new UTF8Encoding(false));
        using var comparison = new StreamWriter(Path.Combine(output, "parity-production-mirror-comparison.csv"),
            false, new UTF8Encoding(false));
        raw.WriteLine("Scenario,Iteration,Stage,SegmentNumber,Field,RawBits");
        manifest.WriteLine("Scenario,Iteration,FieldCount,SHA256");
        comparison.WriteLine("Scenario,Iteration,ComparedFields,Disposition");

        for (var i = 0; i < _production.Count; i++)
        {
            var observed = _production[i];
            if (!Same(observed, _steps[i]))
                throw new InvalidOperationException("Production parity changed before evidence writing.");
            var canonical = new StringBuilder();
            foreach (var field in observed.Fields)
            {
                raw.WriteLine(string.Join(",",
                    observed.Scenario,
                    observed.Iteration.ToString(CultureInfo.InvariantCulture),
                    field.Stage,
                    field.Segment.ToString(CultureInfo.InvariantCulture),
                    field.Name,
                    field.Bits));
                canonical.Append(field.Stage).Append("/")
                    .Append(field.Segment.ToString(CultureInfo.InvariantCulture)).Append("/")
                    .Append(field.Name).Append("=").Append(field.Bits).Append('\n');
            }
            var sha = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString())));
            manifest.WriteLine(string.Join(",",
                observed.Scenario,
                observed.Iteration.ToString(CultureInfo.InvariantCulture),
                observed.Fields.Count.ToString(CultureInfo.InvariantCulture),
                sha));
            comparison.WriteLine(string.Join(",",
                observed.Scenario,
                observed.Iteration.ToString(CultureInfo.InvariantCulture),
                observed.Fields.Count.ToString(CultureInfo.InvariantCulture),
                "DIRECT_BITS_EQUAL"));
        }
    }

}
