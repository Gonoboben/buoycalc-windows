using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Services;

// Read-only raw-state survey, never a convergence/acceptance predicate.
internal static class RawCycleSurvey
{
    internal static int Run(string output)
    {
        var summaries = new List<object>();
        var ids = new[] { "grid-20-1.2-0.6", "neighborhood-20-1.19-0.65", "neighborhood-20-1.2-0.65", "neighborhood-380-1.19-0.65", "coeff-0.5-14-1030" };
        foreach (var fixture in AuthorityProofFixtures.Build().Where(f => ids.Contains(f.Id, StringComparer.Ordinal)))
        foreach (var variant in new[] { 0, 1, 2, 3, 4 })
        {
            var known = variant != 0;
            var allowThree = variant >= 2;
            var perpendicular = variant >= 3;
            var forceRatio = variant == 4;
            var run = ApplicationCalculationRunner.Run(fixture.Environment, fixture.Buoy, fixture.Assembly, fixture.Anchor, fixture.SafetyFactor);
            var states = new List<long[]>();
            var transitions = new List<int>();
            var candidate = MooringSignedCandidateEvaluator.BuildWithDiagnosticNumerics(fixture.Environment, fixture.Buoy,
                run.Result, run.Snapshot.TechnicalReportData.SequencePositions, false, false,
                recoverExactTwoCycles: true, recoverSquaredNormalCycles: true, recoverKnownBuoyCycles: known, recoverExactThreeCycles: allowThree, recoverPerpendicularNormalCycles: perpendicular, useNormalForceRatio: forceRatio,
                recoveryObserver: (i, _) => transitions.Add(i), rawStateObserver: (i, state) =>
                {
                    if (i != states.Count) throw new InvalidOperationException("Nonconsecutive raw observation.");
                    states.Add(state.ToArray());
                });
            var replayStates = new List<long[]>();
            var replay = MooringSignedCandidateEvaluator.BuildWithDiagnosticNumerics(fixture.Environment, fixture.Buoy,
                run.Result, run.Snapshot.TechnicalReportData.SequencePositions, false, false,
                recoverExactTwoCycles: true, recoverSquaredNormalCycles: true, recoverKnownBuoyCycles: known, recoverExactThreeCycles: allowThree, recoverPerpendicularNormalCycles: perpendicular, useNormalForceRatio: forceRatio,
                rawStateObserver: (_, state) => replayStates.Add(state.ToArray()));
            var unobserved = MooringSignedCandidateEvaluator.BuildWithDiagnosticNumerics(fixture.Environment, fixture.Buoy,
                run.Result, run.Snapshot.TechnicalReportData.SequencePositions, false, false,
                recoverExactTwoCycles: true, recoverSquaredNormalCycles: true, recoverKnownBuoyCycles: known, recoverExactThreeCycles: allowThree, recoverPerpendicularNormalCycles: perpendicular, useNormalForceRatio: forceRatio);
            if (ObserverControlAudit.Canonical(candidate) != ObserverControlAudit.Canonical(replay) ||
                ObserverControlAudit.Canonical(candidate) != ObserverControlAudit.Canonical(unobserved) ||
                states.Count != replayStates.Count || states.Where((state, i) => !state.SequenceEqual(replayStates[i])).Any())
                throw new InvalidOperationException("Raw-state observer/replay changed a candidate or raw trajectory.");
            var lastMapStart = transitions.Count == 0 ? 0 : transitions[^1];
            var period = 0;
            for (var p = 1; p < states.Count - lastMapStart; p++)
            {
                if (states[^1].SequenceEqual(states[states.Count - 1 - p])) { period = p; break; }
            }
            var mode = forceRatio ? "H2RF3Q" : perpendicular ? "H2RF3P" : allowThree ? "H2RF3" : known ? "H2RF" : "H2R3";
            if (period > 0)
            {
                using var writer = new StreamWriter(Path.Combine(output, fixture.Id + "-" + mode + "-terminal-period.csv"), false, new UTF8Encoding(false));
                var first = states.Count - 1 - period;
                writer.WriteLine("FieldIndex," + string.Join(",", Enumerable.Range(first, period + 1).Select(i => "Iteration" + i)));
                for (var f = 0; f < states[^1].Length; f++)
                    writer.WriteLine(f + "," + string.Join(",", Enumerable.Range(first, period + 1).Select(i => states[i][f].ToString("X16", CultureInfo.InvariantCulture))));
            }
            summaries.Add(new { fixture.Id, Mode = mode, Status = candidate.Status.ToString(), candidate.FeedbackIterations,
                LastMapStart = lastMapStart, TerminalRawPeriod = period, RawReplayIdentical = true, ObserverNoninterference = true,
                States = states.Select((state, i) => new { Iteration = i, FieldCount = state.Length,
                    SHA256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\n", state.Select(v => v.ToString("X16", CultureInfo.InvariantCulture)))))) }).ToArray() });
            File.WriteAllText(Path.Combine(output, "raw-cycle-summary.json"), JsonSerializer.Serialize(summaries, new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine($"RAW_CYCLE|{fixture.Id}|{mode}|{candidate.Status}|{period}");
        }
        return 0;
    }
}
