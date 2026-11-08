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
        var ids = new[] { "grid-20-1.2-0.6", "neighborhood-20-1.19-0.65", "neighborhood-20-1.2-0.65",
            "neighborhood-380-1.19-0.65", "coeff-0.5-14-1030", AuthorityProofFixtures.AlternateControl,
            AuthorityProofFixtures.BudgetControl };
        foreach (var fixture in AuthorityProofFixtures.Build().Where(f => ids.Contains(f.Id, StringComparer.Ordinal)))
        foreach (var variant in new[] { 0, 1, 2, 3, 4, 5, 6 })
        {
            var known = variant != 0;
            var allowThree = variant >= 2;
            var perpendicular = variant >= 3;
            var forceRatio = variant >= 4;
            var canonical = variant >= 5;
            var alternate = variant == 6;
            var run = ApplicationCalculationRunner.Run(fixture.Environment, fixture.Buoy, fixture.Assembly, fixture.Anchor, fixture.SafetyFactor);
            var states = new List<long[]>();
            var transitions = new List<(int Iteration, string Stage)>();
            var resets = new List<(int Iteration, long[] Bits)>();
            var candidate = MooringSignedCandidateEvaluator.BuildWithDiagnosticNumerics(fixture.Environment, fixture.Buoy,
                run.Result, run.Snapshot.TechnicalReportData.SequencePositions, false, false,
                recoverExactTwoCycles: true, recoverSquaredNormalCycles: true, recoverKnownBuoyCycles: known, recoverExactThreeCycles: allowThree, recoverPerpendicularNormalCycles: perpendicular, useNormalForceRatio: forceRatio, recoverCanonicalRestartCycles: canonical,
                canonicalResetObserver: (i, state) =>
                {
                    if (!state.SequenceEqual(states[0])) throw new InvalidOperationException("Reset source differs from initial full physical state.");
                    resets.Add((i, state.ToArray()));
                },
                recoverAlternatePerpendicularCycles: alternate,
                recoveryObserver: (i, stage) => transitions.Add((i, stage)), rawStateObserver: (i, state) =>
                {
                    if (i != states.Count) throw new InvalidOperationException("Nonconsecutive raw observation.");
                    states.Add(state.ToArray());
                });
            var replayStates = new List<long[]>();
            var replayTransitions = new List<(int Iteration, string Stage)>();
            var replay = MooringSignedCandidateEvaluator.BuildWithDiagnosticNumerics(fixture.Environment, fixture.Buoy,
                run.Result, run.Snapshot.TechnicalReportData.SequencePositions, false, false,
                recoverExactTwoCycles: true, recoverSquaredNormalCycles: true, recoverKnownBuoyCycles: known, recoverExactThreeCycles: allowThree, recoverPerpendicularNormalCycles: perpendicular, useNormalForceRatio: forceRatio, recoverCanonicalRestartCycles: canonical,
                canonicalResetObserver: (_, state) =>
                { if (!state.SequenceEqual(states[0])) throw new InvalidOperationException("Replay reset source differs."); },
                recoverAlternatePerpendicularCycles: alternate,
                recoveryObserver: (i, stage) => replayTransitions.Add((i, stage)),
                rawStateObserver: (_, state) => replayStates.Add(state.ToArray()));
            var unobserved = MooringSignedCandidateEvaluator.BuildWithDiagnosticNumerics(fixture.Environment, fixture.Buoy,
                run.Result, run.Snapshot.TechnicalReportData.SequencePositions, false, false,
                recoverExactTwoCycles: true, recoverSquaredNormalCycles: true, recoverKnownBuoyCycles: known, recoverExactThreeCycles: allowThree, recoverPerpendicularNormalCycles: perpendicular, useNormalForceRatio: forceRatio, recoverCanonicalRestartCycles: canonical,
                recoverAlternatePerpendicularCycles: alternate);
            if (ObserverControlAudit.Canonical(candidate) != ObserverControlAudit.Canonical(replay) ||
                ObserverControlAudit.Canonical(candidate) != ObserverControlAudit.Canonical(unobserved) ||
                !transitions.SequenceEqual(replayTransitions) || states.Count != replayStates.Count ||
                states.Where((state, i) => !state.SequenceEqual(replayStates[i])).Any())
                throw new InvalidOperationException("Raw-state observer/replay changed a candidate or raw trajectory.");
            var lastMapStart = transitions.Count == 0 ? 0 : transitions[^1].Iteration;
            var period = 0;
            for (var p = 1; p < states.Count - lastMapStart; p++)
            {
                if (states[^1].SequenceEqual(states[states.Count - 1 - p])) { period = p; break; }
            }
            var mode = alternate ? "H2RF3QPK" : canonical ? "H2RF3QK" : forceRatio ? "H2RF3Q" : perpendicular ? "H2RF3P" : allowThree ? "H2RF3" : known ? "H2RF" : "H2R3";
            if (period > 0)
                WriteWindow("terminal-period", states.Count - 1 - period, period);
            var transitionWitnesses = new List<object>();
            if (alternate)
            {
                var mapStart = 0;
                foreach (var (iteration, stage) in transitions)
                {
                    var cyclePeriod = iteration - 2 >= mapStart && states[iteration - 2].SequenceEqual(states[iteration]) ? 2 :
                        iteration - 3 >= mapStart && states[iteration - 3].SequenceEqual(states[iteration]) ? 3 : 0;
                    if (cyclePeriod == 0)
                        throw new InvalidOperationException($"{fixture.Id}/{stage}: no repeated complete state in the active map.");
                    WriteWindow($"transition-{iteration}", iteration - cyclePeriod, cyclePeriod);
                    transitionWitnesses.Add(new { Iteration = iteration, Stage = stage, Period = cyclePeriod });
                    mapStart = iteration;
                }
            }
            void WriteWindow(string suffix, int first, int length)
            {
                using var writer = new StreamWriter(Path.Combine(output, $"{fixture.Id}-{mode}-{suffix}.csv"), false, new UTF8Encoding(false));
                var indexes = Enumerable.Range(first, length + 1).ToArray();
                writer.WriteLine("FieldIndex," + string.Join(",", indexes.Select(i => "Iteration" + i)));
                for (var f = 0; f < states[first].Length; f++)
                    writer.WriteLine(f + "," + string.Join(",", indexes.Select(i => states[i][f].ToString("X16", CultureInfo.InvariantCulture))));
            }
            if (resets.Count > 0)
            {
                using var writer = new StreamWriter(Path.Combine(output, fixture.Id + "-" + mode + "-restart-source.csv"), false, new UTF8Encoding(false));
                writer.WriteLine("FieldIndex,InitialRawBits,ResetRawBits");
                for (var f = 0; f < states[0].Length; f++)
                    writer.WriteLine($"{f},{states[0][f]:X16},{resets[0].Bits[f]:X16}");
            }
            summaries.Add(new { fixture.Id, Mode = mode, Status = candidate.Status.ToString(), candidate.FeedbackIterations,
                LastMapStart = lastMapStart, TerminalRawPeriod = period, TransitionWitnesses = transitionWitnesses,
                CanonicalResets = resets.Select(reset => new { reset.Iteration, SourceStateIteration = 0,
                    FieldCount = reset.Bits.Length, RawBitsMatchState0 = true,
                    SourceSHA256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\n", reset.Bits.Select(v => v.ToString("X16", CultureInfo.InvariantCulture)))))) }).ToArray(), RawReplayIdentical = true, ObserverNoninterference = true,
                States = states.Select((state, i) => new { Iteration = i, FieldCount = state.Length,
                    SHA256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\n", state.Select(v => v.ToString("X16", CultureInfo.InvariantCulture)))))) }).ToArray() });
            File.WriteAllText(Path.Combine(output, "raw-cycle-summary.json"), JsonSerializer.Serialize(summaries, new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine($"RAW_CYCLE|{fixture.Id}|{mode}|{candidate.Status}|{period}");
        }
        return 0;
    }
}
