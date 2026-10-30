using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

// BC-AUD-006: validation-only bit-state research. This type has NO production consumers.
internal sealed record ExactCycleField(string Name, string Bits);
internal sealed record ExactCycleSample(string Scenario, int Iteration, IReadOnlyList<ExactCycleField> Fields);

internal sealed class ExactCycleResearchReport
{
    private const int ExpectedBudget = MooringSignedCandidateResult.ProductionFeedbackBudget;
    private readonly List<string> _summaries = new();
    private readonly List<ExactCycleSample> _fullStates = new();
    private readonly List<string> _observerControlEvidence = new()
    {
        "Scenario,ExpectedStatus,ObserverSteps,CandidateFieldsIdentical,SelectedAuthorityUnchanged,OriginalSHA256,ObservedSHA256"
    };
    public IReadOnlyList<string> ObserverControlEvidence => _observerControlEvidence.AsReadOnly();
    private readonly List<string> _differences = new()
    {
        "Scenario,FirstStateIteration,SecondStateIteration,Field,FirstBits,SecondBits"
    };

    // Direct, ordered field equality is the authority for cycle detection.
    // Fingerprints are emitted as diagnostic indexes, never as proof of equality.
    public static ExactCycleSample Capture(string scenario, int iteration, FixedFeedbackState state)
    {
        var fields = new List<ExactCycleField>();

        static string Bits(double value) =>
            BitConverter.DoubleToInt64Bits(value).ToString("X16", CultureInfo.InvariantCulture);
        void Scalar(string name, double value) => fields.Add(new ExactCycleField(name, Bits(value)));
        void MaybeScalar(string name, double? value) =>
            fields.Add(new ExactCycleField(name, value.HasValue ? Bits(value.Value) : "null"));
        void Integer(string name, int value) => fields.Add(new ExactCycleField(name, value.ToString(CultureInfo.InvariantCulture)));
        void Symbol(string name, string value) => fields.Add(new ExactCycleField(name, value));

        Symbol("Boundary.Classification", state.Boundary.Classification.ToString());
        MaybeScalar("Boundary.Q0N", state.Boundary.Q0N);
        MaybeScalar("Boundary.EndpointXM", state.Boundary.SolutionState?.EndpointXM);
        MaybeScalar("Boundary.EndpointZM", state.Boundary.SolutionState?.EndpointZM);
        Scalar("LineForceN", state.LineForceN);
        Integer("Trace.PointLoadCrossings", state.Trace.PointLoadCrossings);
        Integer("Geometry.NodeCount", state.Geometry.Nodes.Count);
        Integer("Result.SegmentCount", state.Result.SegmentRows.Count);
        Integer("Trace.RowCount", state.Trace.Rows.Count);

        for (var i = 0; i < state.Geometry.Nodes.Count; i++)
        {
            Scalar($"Geometry.Nodes[{i}].X", state.Geometry.Nodes[i].X);
            Scalar($"Geometry.Nodes[{i}].Z", state.Geometry.Nodes[i].Z);
        }

        foreach (var segment in state.Result.SegmentRows.OrderBy(s => s.Number))
        {
            var key = $"Segments[{segment.Number}]";
            Integer(key + ".Number", segment.Number);
            Scalar(key + ".CurrentForceN", segment.CurrentForceN);
        }

        foreach (var row in state.Trace.Rows)
        {
            var key = $"Trace.Rows[{row.SegmentNumber}]";
            Integer(key + ".SegmentNumber", row.SegmentNumber);
            Scalar(key + ".StartLengthM", row.StartLengthM);
            Scalar(key + ".EndLengthM", row.EndLengthM);
            Integer(key + ".PointLoadCrossingsAppliedBeforeSegment", row.PointLoadCrossingsAppliedBeforeSegment);
            Scalar(key + ".StartHN", row.StartHN);
            Scalar(key + ".StartVN", row.StartVN);
            Scalar(key + ".MidHN", row.MidHN);
            Scalar(key + ".MidVN", row.MidVN);
            Scalar(key + ".EndHN", row.EndHN);
            Scalar(key + ".EndVN", row.EndVN);
            Scalar(key + ".MidTensionN", row.MidTensionN);
            MaybeScalar(key + ".TangentX", row.TangentX);
            MaybeScalar(key + ".TangentZ", row.TangentZ);
        }

        if (fields.Select(f => f.Name).Distinct(StringComparer.Ordinal).Count() != fields.Count)
            throw new InvalidOperationException("BC-AUD-006: duplicated bit-field key.");

        return new ExactCycleSample(scenario, iteration, fields);
    }

    public void Analyze(string scenario, IReadOnlyList<ExactCycleSample> first, IReadOnlyList<ExactCycleSample> replay)
    {
        if (first.Count != ExpectedBudget + 1 || replay.Count != first.Count)
            throw new InvalidOperationException($"{scenario}: expected state 0 through {ExpectedBudget} in both diagnostic replays.");

        for (var i = 0; i < first.Count; i++)
        {
            if (first[i].Iteration != i || replay[i].Iteration != i ||
                first[i].Scenario != scenario || replay[i].Scenario != scenario ||
                !Same(first[i], replay[i]))
                throw new InvalidOperationException($"{scenario}: exact-bit replay mismatch at iteration {i}.");
        }

        _fullStates.AddRange(first);
        var firstCycleRepeat = FindFirstPeriodTwo(first);
        if (firstCycleRepeat < 2)
            throw new InvalidOperationException($"{scenario}: expected exact field-by-field period-2 repetition, none found.");

        for (var i = firstCycleRepeat; i < first.Count; i++)
        {
            if (!Same(first[i], first[i - 2]) || Same(first[i], first[i - 1]))
                throw new InvalidOperationException($"{scenario}: period-2 orbit did not persist at state {i}.");
        }

        var a = first[firstCycleRepeat - 1];
        var b = first[firstCycleRepeat];
        var different = new List<ExactCycleField>();
        for (var i = 0; i < a.Fields.Count; i++)
        {
            var left = a.Fields[i];
            var right = b.Fields[i];
            if (left.Name != right.Name)
                throw new InvalidOperationException($"{scenario}: field ordering changed in period-2 states.");

            if (!StringComparer.Ordinal.Equals(left.Bits, right.Bits))
            {
                different.Add(left);
                _differences.Add(string.Join(",",
                    scenario,
                    a.Iteration.ToString(CultureInfo.InvariantCulture),
                    b.Iteration.ToString(CultureInfo.InvariantCulture),
                    left.Name,
                    left.Bits,
                    right.Bits));
            }
        }

        if (different.Count == 0)
            throw new InvalidOperationException($"{scenario}: alleged period-2 states are bit-identical.");
        if (!Same(first[^1], first[^3]) || Same(first[^1], first[^2]))
            throw new InvalidOperationException($"{scenario}: terminal state is not a strict period-2 orbit.");

        var summary = string.Join("|",
            "BC_AUD_006_CYCLE",
            $"Scenario={scenario}",
            $"FirstRepeatedState={firstCycleRepeat}",
            $"AlternatingState={firstCycleRepeat - 1}",
            "Period=2",
            $"Fields={a.Fields.Count}",
            $"BitDifferentFields={different.Count}",
            "RepeatRunBitIdentical=True",
            "PeriodStableThrough64=True",
            "ProductionDisposition=BudgetExhausted",
            "CanonicalRepresentative=NOT_AUTHORIZED",
            "F1F2F3F4Equivalence=NOT_PROVEN",
            "SolverModified=False");
        _summaries.Add(summary);
        Console.WriteLine(summary);
    }

    public void VerifyProductionControls()
    {
        // Synthetic negative controls: drifting and fixed-point states are NOT strict period-2 cycles.
        ExactCycleSample Sample(int i, double value) =>
            new("synthetic", i, new[] { new ExactCycleField("Probe", BitConverter.DoubleToInt64Bits(value).ToString("X16", CultureInfo.InvariantCulture)) });
        if (FindFirstPeriodTwo(new[] { Sample(0, 1), Sample(1, 2), Sample(2, 3), Sample(3, 4) }) >= 0 ||
            FindFirstPeriodTwo(new[] { Sample(0, 3), Sample(1, 3), Sample(2, 3) }) >= 0 ||
            FindFirstPeriodTwo(new[] { Sample(0, 1), Sample(1, 2), Sample(2, 1), Sample(3, 2) }) != 2)
            throw new InvalidOperationException("BC-AUD-006: exact period detector failed synthetic positive/negative controls.");

        var rejected = RunProductionControl("A03-rejected", 20.0, 20.0, 0.2);
        if (rejected.Snapshot.SignedCandidate?.Status != MooringSignedCandidateStatus.RejectedPhysical ||
            rejected.Snapshot.SelectedShape is not null)
            throw new InvalidOperationException("BC-AUD-006: physical rejection control was downgraded.");

        var accepted = RunProductionControl("A05-accepted", 20.0, 20.2, 0.2);
        if (accepted.Snapshot.SignedCandidate?.Status != MooringSignedCandidateStatus.Accepted ||
            !accepted.Snapshot.SignedCandidate.ExactFixedPointReached ||
            accepted.Snapshot.SelectedShape is null ||
            accepted.Snapshot.SelectedDesignTensionDemand is null ||
            accepted.Snapshot.SelectedAnchorReaction is null ||
            accepted.Snapshot.SelectedLocalStructuralCapacity is null ||
            accepted.Snapshot.SelectedEngineeringAssessment is null)
            throw new InvalidOperationException("BC-AUD-006: known Accepted/F1-F4 control failed.");

        if (_observerControlEvidence.Count != 3)
            throw new InvalidOperationException("BC-AUD-006: missing A03/A05 observer-control evidence.");
        Console.WriteLine("BC_AUD_006_CONTROLS|NonPeriodicNotAccepted=True|OneStateNotPeriodTwo=True|PeriodTwoRecognized=True|RejectedPhysicalPreserved=True|AcceptedF1F2F3F4Preserved=True|A03ObserverParity=True|A05ObserverParity=True");
    }

    private static ApplicationCalculationRun RunProductionControl(string title, double depth, double length, double current)
    {
        var env = new EnvironmentInput(
            1025.0, depth, 0.0, 0.0, 0.0,
            new SeabedPreset("campaign:sand", "Validation sand", 1.2, "Validation diagnostic only."),
            true,
            new[]
            {
                new CurrentProfilePointInput(0.0, current, 0.0, 0.0, 1025.0),
                new CurrentProfilePointInput(depth, current, 0.0, 0.0, 1025.0)
            });
        var buoy = new BuoyInput("Series A reference buoy", 1.0, 100.0, 0.10, 0.8);
        var rope = new RopePreset("campaign:rope-14", "Series A 14 mm line",
            "Synthetic reference", 14.0, 70.0, 0.15, 1.0, "Validation diagnostic only.");
        var anchor = new AnchorInput("Series A 3000 kg block", "Concrete block", "Concrete", 3000.0, 1.2, 1.0);
        var assembly = new[]
        {
            new AssemblyItemInput(AssemblyItemKind.Line, title, true, rope, null, length, 1, 0.0, 0.0, 0.0, 0.0)
        };
        var run = ApplicationCalculationRunner.Run(env, buoy, assembly, anchor, 3.0);
        var expectedStatus = title.StartsWith("A03", StringComparison.Ordinal)
            ? MooringSignedCandidateStatus.RejectedPhysical
            : title.StartsWith("A05", StringComparison.Ordinal)
                ? MooringSignedCandidateStatus.Accepted
                : throw new InvalidOperationException("Unexpected production control scenario.");
        _observerControlEvidence.Add(
            ObserverControlAudit.Verify(title, env, buoy, run, expectedStatus));
        return run;
    }

    public void Write(string output)
    {
        if (_summaries.Count != 3 || _differences.Count <= 1)
            throw new InvalidOperationException("BC-AUD-006: incomplete scenario evidence.");
        File.WriteAllLines(Path.Combine(output, "exact-cycle-bit-differences.csv"), _differences, Encoding.UTF8);
        WriteFullTrajectory(output);
        var report = new StringBuilder();
        report.AppendLine("# BC-AUD-006 exact-bit cycle validation (research only)");
        report.AppendLine();
        report.AppendLine("Observed using the existing validation mirror of production public feedback stages.");
        report.AppendLine("A separate production run confirms BudgetExhausted; this mirror is not an independent physical solver.");
        report.AppendLine("Cycle identity is evaluated by direct ordered field-by-field bit comparison, not hash equality or epsilon.");
        report.AppendLine("All states (0..64) were compared against a second fresh deterministic replay.");
        report.AppendLine("Full named bit states: exact-cycle-full-states.csv. Per-state SHA-256: exact-cycle-state-sha256.csv.");
        report.AppendLine("Digest canonical bytes: UTF-8 of each ordered Field=RawBits followed by LF (U+000A), without BOM or other separators. Split full-state CSV by Scenario and Iteration and recompute independently.");
        report.AppendLine();
        foreach (var line in _summaries) report.AppendLine("- " + line);
        report.AppendLine();
        report.AppendLine("The differences CSV contains the raw IEEE-754 bit patterns of every differing field in the first strict period-2 pair.");
        report.AppendLine("F1-F4 equivalence between orbit members has NOT been demonstrated. No canonical state is selected.");
        report.AppendLine("No production convergence semantics, equations, iteration budget, baseline, DTO or release status were changed.");
        File.WriteAllText(Path.Combine(output, "exact-cycle-research.md"), report.ToString(), Encoding.UTF8);
    }

    private void WriteFullTrajectory(string output)
    {
        if (_fullStates.Count != 3 * (ExpectedBudget + 1) ||
            _fullStates.GroupBy(s => s.Scenario).Count() != 3 ||
            _fullStates.GroupBy(s => s.Scenario).Any(g =>
                g.Count() != ExpectedBudget + 1 ||
                !g.Select(s => s.Iteration).SequenceEqual(Enumerable.Range(0, ExpectedBudget + 1))))
            throw new InvalidOperationException("BC-AUD-006: full trajectory incomplete.");

        using var full = new StreamWriter(Path.Combine(output, "exact-cycle-full-states.csv"), false, new UTF8Encoding(false));
        using var manifest = new StreamWriter(Path.Combine(output, "exact-cycle-state-sha256.csv"), false, new UTF8Encoding(false));
        full.WriteLine("Scenario,Iteration,Field,RawBits");
        manifest.WriteLine("Scenario,Iteration,FieldCount,SHA256");
        foreach (var state in _fullStates)
        {
            if (state.Fields.Count == 0) throw new InvalidOperationException("BC-AUD-006: empty state.");
            var canonical = new StringBuilder();
            foreach (var field in state.Fields)
            {
                if (field.Name.Contains(",") || field.Name.Contains("=") || field.Name.Contains("\n") ||
                    field.Bits.Contains(",") || field.Bits.Contains("\n"))
                    throw new InvalidOperationException("BC-AUD-006: invalid canonical field characters.");
                full.WriteLine(string.Join(",", state.Scenario,
                    state.Iteration.ToString(CultureInfo.InvariantCulture), field.Name, field.Bits));
                canonical.Append(field.Name).Append("=").Append(field.Bits).Append("\n");
            }
            var sha = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString())));
            manifest.WriteLine(string.Join(",", state.Scenario,
                state.Iteration.ToString(CultureInfo.InvariantCulture),
                state.Fields.Count.ToString(CultureInfo.InvariantCulture), sha));
        }
    }
    private static int FindFirstPeriodTwo(IReadOnlyList<ExactCycleSample> states)
    {
        for (var i = 2; i < states.Count; i++)
        {
            if (Same(states[i], states[i - 2]) && !Same(states[i], states[i - 1]))
                return i;
        }
        return -1;
    }

    internal static bool Same(ExactCycleSample a, ExactCycleSample b)
    {
        if (a.Fields.Count != b.Fields.Count) return false;
        for (var i = 0; i < a.Fields.Count; i++)
        {
            if (a.Fields[i].Name != b.Fields[i].Name ||
                !StringComparer.Ordinal.Equals(a.Fields[i].Bits, b.Fields[i].Bits))
                return false;
        }
        return true;
    }
}
