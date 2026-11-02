using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

// This entire implementation is diagnostic. Nothing here can update the solver or its authority.
internal sealed class IsolationExperimentStop : Exception
{
    public IsolationExperimentStop(string mode, int iteration, string reason, string detail)
        : base($"{mode}|{iteration}|{reason}|{detail}")
    {
        Mode = mode;
        Iteration = iteration;
        Reason = reason;
        Detail = detail;
    }
    public string Mode { get; }
    public int Iteration { get; }
    public string Reason { get; }
    public string Detail { get; }
}

internal sealed class RootCauseIsolationResearch
{
    private const int Budget = MooringSignedCandidateResult.ProductionFeedbackBudget;
    private readonly string _working;
    private readonly StreamWriter _full;
    private readonly StreamWriter _manifest;
    private readonly StreamWriter _outcomes;
    private readonly StreamWriter _first;
    private readonly StreamWriter _stages;
    private int _experiments;

    public RootCauseIsolationResearch(string output)
    {
        _working = output + "-work";
        Directory.CreateDirectory(_working);
        _full = Start("full-states.csv", "Scenario,Mode,Iteration,Field,RawBits");
        _manifest = Start("state-sha256.csv", "Scenario,Mode,Iteration,FieldCount,SHA256");
        _outcomes = Start("outcomes.csv", "Scenario,Mode,ObservedSteps,Outcome,FirstExactFixedPoint,TerminalStrictPeriod,ReplayIdentical,StopIteration,StopReason");
        _first = Start("first-differences.csv", "Scenario,Mode,Iteration,Field,BaselineBits,ExperimentalBits");
        _stages = Start("first-stage-differences.csv", "Scenario,Mode,Iteration,Stage,SegmentNumber,Field,BaselineBits,ExperimentalBits");
    }

    private StreamWriter Start(string file, string header)
    {
        var writer = new StreamWriter(Path.Combine(_working, file), false, new UTF8Encoding(false));
        writer.WriteLine(header);
        return writer;
    }

    public void AnalyzeScenario(
        FixedDiagnosticScenario scenario,
        EnvironmentInput environment,
        BuoyInput buoy,
        CalculationResult baseResult,
        MooringSequencePositionResult sequence,
        IReadOnlyList<ExactCycleSample> baseline,
        IReadOnlyList<ParityStepSample> baselineStages)
    {
        if (baseline.Count != Budget + 1 || baselineStages.Count != Budget ||
            baseline.Where((sample, index) => sample.Iteration != index).Any())
            throw new InvalidOperationException($"{scenario.Id}: missing complete ordered B0 baseline.");
        WriteStates(scenario.Id, "B0", baseline);
        _outcomes.WriteLine($"{scenario.Id},B0,64,StrictPeriod2,,2,True,,");
        _experiments++;
        var variants = scenario.Id == "A15"
            ? new[] { "H1", "H1N1", "H1N2", "H2", "H1H2" }
            : new[] { "H2" };
        foreach (var mode in variants)
        {
            var first = Run(scenario, environment, buoy, baseResult, sequence, mode);
            var replay = Run(scenario, environment, buoy, baseResult, sequence, mode);
            if (first.Samples.Count != replay.Samples.Count ||
                first.Stages.Count != replay.Stages.Count ||
                first.Rows.Count != replay.Rows.Count ||
                first.Stop?.Iteration != replay.Stop?.Iteration ||
                first.Stop?.Reason != replay.Stop?.Reason ||
                first.Stop?.Detail != replay.Stop?.Detail)
                throw new InvalidOperationException($"{scenario.Id}/{mode}: replay structural mismatch.");
            for (var i = 0; i < first.Samples.Count; i++)
            {
                if (!ExactCycleResearchReport.Same(first.Samples[i], replay.Samples[i]))
                    throw new InvalidOperationException($"{scenario.Id}/{mode}: replay state bits mismatch at {i}.");
            }
            for (var i = 0; i < first.Stages.Count; i++)
            {
                var a = first.Stages[i].Fields;
                var b = replay.Stages[i].Fields;
                if (a.Count != b.Count ||
                    a.Where((field, index) =>
                        field.Stage != b[index].Stage || field.Segment != b[index].Segment ||
                        field.Name != b[index].Name || field.Bits != b[index].Bits).Any())
                    throw new InvalidOperationException($"{scenario.Id}/{mode}: stage replay mismatch at {i+1}.");
            }
            WriteStates(scenario.Id, mode, first.Samples);
            FirstStateDifference(scenario.Id, mode, baseline, first.Samples);
            FirstStageDifference(scenario.Id, mode, baselineStages, first.Stages);
            var firstFixed = first.Rows.FirstOrDefault(row => row.ExactFixedPoint)?.Iteration;
            var lastPeriod = first.Rows.LastOrDefault()?.StrictPeriod;
            var outcome = first.Stop is not null ? first.Stop.Reason
                : firstFixed.HasValue ? "ExactFixedPointObserved"
                : lastPeriod == 2 ? "StrictPeriod2" : "OtherNonaccepted";
            _outcomes.WriteLine(string.Join(",",
                scenario.Id, mode,
                (first.Samples.Count - 1).ToString(CultureInfo.InvariantCulture),
                outcome,
                firstFixed?.ToString(CultureInfo.InvariantCulture) ?? "",
                lastPeriod?.ToString(CultureInfo.InvariantCulture) ?? "",
                "True",
                first.Stop?.Iteration.ToString(CultureInfo.InvariantCulture) ?? "",
                first.Stop?.Reason ?? ""));
            _experiments++;
            Console.WriteLine($"BC_AUD_006_ISOLATION|Scenario={scenario.Id}|Mode={mode}|Outcome={outcome}|Steps={first.Samples.Count - 1}|FirstFixed={firstFixed?.ToString() ?? "none"}|ReplayBitsIdentical=True|ResearchOnly=True");
        }
    }

    private static (List<ExactCycleSample> Samples, List<ParityStepSample> Stages,
        IReadOnlyList<FixedIterationRow> Rows, IsolationExperimentStop? Stop) Run(
        FixedDiagnosticScenario scenario,
        EnvironmentInput environment,
        BuoyInput buoy,
        CalculationResult baseResult,
        MooringSequencePositionResult sequence,
        string mode)
    {
        var samples = new List<ExactCycleSample>();
        var stages = new List<ParityStepSample>();
        IReadOnlyList<FixedIterationRow> rows = Array.Empty<FixedIterationRow>();
        IsolationExperimentStop? stop = null;
        try
        {
            rows = FixedProgram.Diagnose(
                scenario, environment, buoy, baseResult, sequence,
                (iteration, state) => samples.Add(
                    ExactCycleResearchReport.Capture(scenario.Id, iteration, state)),
                step => stages.Add(ParityReadoutReport.Capture(
                    step, allowDiagnosticBuoyOverride: mode is "H1" or "H1H2" or "H1N1" or "H1N2")), mode);
        }
        catch (IsolationExperimentStop ex) { stop = ex; }
        if (samples.Count == 0 || samples[0].Iteration != 0 ||
            samples.Where((sample, index) => sample.Iteration != index).Any())
            throw new InvalidOperationException($"{scenario.Id}/{mode}: missing ordered state trajectory.");
        if (stop is null && (samples.Count != Budget + 1 || stages.Count != Budget || rows.Count != Budget))
            throw new InvalidOperationException($"{scenario.Id}/{mode}: incomplete 64-step trajectory.");
        if (stop is not null && (samples.Count != stop.Iteration || stages.Count != stop.Iteration-1))
            throw new InvalidOperationException($"{scenario.Id}/{mode}: truncated stop/state evidence.");
        return (samples, stages, rows, stop);
    }

    private void WriteStates(string scenario, string mode, IReadOnlyList<ExactCycleSample> states)
    {
        foreach (var state in states)
        {
            var canonical = new StringBuilder();
            foreach (var field in state.Fields)
            {
                _full.WriteLine(string.Join(",",
                    scenario, mode, state.Iteration.ToString(CultureInfo.InvariantCulture), field.Name, field.Bits));
                canonical.Append(field.Name).Append('=').Append(field.Bits).Append('\n');
            }
            var digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString())));
            _manifest.WriteLine(string.Join(",",
                scenario, mode, state.Iteration.ToString(CultureInfo.InvariantCulture),
                state.Fields.Count.ToString(CultureInfo.InvariantCulture), digest));
        }
    }

    private void FirstStateDifference(string scenario, string mode,
        IReadOnlyList<ExactCycleSample> baseline, IReadOnlyList<ExactCycleSample> variant)
    {
        for (var i = 0; i < variant.Count; i++)
        {
            var a = baseline[i].Fields;
            var b = variant[i].Fields;
            if (a.Count != b.Count)
                throw new InvalidOperationException("Isolation state schema mismatch.");
            for (var j = 0; j < a.Count; j++)
            {
                if (a[j].Name != b[j].Name)
                    throw new InvalidOperationException("Isolation state field identity mismatch.");
                if (a[j].Bits != b[j].Bits)
                {
                    _first.WriteLine(string.Join(",", scenario, mode, i,
                        a[j].Name, a[j].Bits, b[j].Bits));
                    return;
                }
            }
        }
        _first.WriteLine($"{scenario},{mode},,,,");
    }

    private void FirstStageDifference(string scenario, string mode,
        IReadOnlyList<ParityStepSample> baseline, IReadOnlyList<ParityStepSample> variant)
    {
        for (var i = 0; i < variant.Count; i++)
        {
            var a = baseline[i].Fields;
            var b = variant[i].Fields;
            if (a.Count != b.Count)
                throw new InvalidOperationException("Isolation stage schema mismatch.");
            for (var j = 0; j < a.Count; j++)
            {
                if (a[j].Stage != b[j].Stage || a[j].Segment != b[j].Segment ||
                    a[j].Name != b[j].Name)
                    throw new InvalidOperationException("Isolation stage field identity mismatch.");
                if (a[j].Bits != b[j].Bits)
                {
                    _stages.WriteLine(string.Join(",", scenario, mode, i + 1,
                        a[j].Stage, a[j].Segment, a[j].Name, a[j].Bits, b[j].Bits));
                    return;
                }
            }
        }
        _stages.WriteLine($"{scenario},{mode},,,,,,");
    }

    public void Finish()
    {
        _full.Dispose();
        _manifest.Dispose();
        _outcomes.Dispose();
        _first.Dispose();
        _stages.Dispose();
        if (_experiments != 10)
            throw new InvalidOperationException($"Expected 10 A06/A11/A15 mode runs, got {_experiments}.");
        var method = string.Join("\n", new[]
        {
            "# BC-AUD-006 64-step ROOT-CAUSE-ISOLATION; research only",
            "B0: original mirror already directly bit-verified against production.",
            "H1: original constant buoy drag via internal boundary input.",
            "H1N1: diagnostic total-(line+point); H1N2: (total-point)-line.",
            "H2: direct tension-trace TangentX/Z in shared force law.",
            "H1H2: combined intervention.",
            "A15: H1, H1N1, H1N2, H2, H1H2; A06/A11: H2.",
            "Identical Series A inputs, 0.20m segmentation, signed weights, 64 feedback iterations.",
            "Alternative state bit replays must match. Stops are reported, never padded.",
            "State hash: UTF-8 Field=RawBits followed by LF in original field order.",
            "No convergence epsilon or canonical representative.",
            "ExactFixedPointObserved is NOT production Accepted, F1-F4 equivalence or physical validation.",
            "First differences are earliest observed deviations, not proven causes.",
            "This legacy-map isolation experiment alone does not resolve Issue #602; see the separately validated production recovery. Release readiness is a separate gate.",
            ""
        });
        File.WriteAllText(Path.Combine(_working, "method.md"), method, new UTF8Encoding(false));
        var outZip = _working + ".zip";
        if (File.Exists(outZip)) File.Delete(outZip);
        ZipFile.CreateFromDirectory(_working, outZip, CompressionLevel.Optimal, false);
        Directory.Delete(_working, true);
        Console.WriteLine($"BC_AUD_006_ISOLATION_COMPLETE|Cases={_experiments}|ZIP={outZip}|BlanketH2ProductionPromotion=NOT_AUTHORIZED");
    }
}
