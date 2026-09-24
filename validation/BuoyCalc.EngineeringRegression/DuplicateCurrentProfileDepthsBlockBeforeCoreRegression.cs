using System.Globalization;
using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal static class DuplicateCurrentProfileDepthsBlockBeforeCoreRegression
{
    private const string BlockingMarker = "BC-AUD-007";

    private static readonly CurrentProfilePointInput A = new(0, 0.1, 0, 0, 1025);
    private static readonly CurrentProfilePointInput B = new(0, 0.8, 0, 0, 1025);
    private static readonly CurrentProfilePointInput C = new(20, 0.2, 0, 0, 1025);

    public static void DuplicateCurrentProfileDepths_BlockBeforeCore()
    {
        Console.WriteLine("BC_AUD_007_DUPLICATE_DEPTH_PRECORE_BEGIN");

        var outcomes = DuplicatePermutations()
            .Select(permutation => Execute(permutation.Name, permutation.Points))
            .ToArray();

        foreach (var outcome in outcomes)
        {
            Console.WriteLine(outcome.CompletedRun is null
                ? $"BC_AUD_007_DUPLICATE_BLOCKED|Permutation={outcome.Name}|CompletedRun=False|Provenance=False|Failure={outcome.Failure?.Message}"
                : string.Join(
                    "|",
                    "BC_AUD_007_PRE_FIX_ACCEPTED",
                    $"Permutation={outcome.Name}",
                    $"CurrentForceN={F(outcome.CompletedRun.Result.CurrentForceN)}",
                    $"Verdict={outcome.CompletedRun.Result.Verdict}",
                    $"SignedStatus={outcome.CompletedRun.Snapshot.SignedCandidate?.Status}",
                    $"RunId={outcome.CompletedRun.Snapshot.Provenance?.RunId}"));
        }

        var completed = outcomes.Where(x => x.CompletedRun is not null).ToArray();
        if (completed.Length > 0)
        {
            var forceCount = completed
                .Select(x => x.CompletedRun!.Result.CurrentForceN)
                .Distinct()
                .Count();
            var statusCount = completed
                .Select(x => x.CompletedRun!.Snapshot.SignedCandidate?.Status)
                .Distinct()
                .Count();
            Console.WriteLine(
                $"BC_AUD_007_PRE_FIX_ROLLUP|CompletedRuns={completed.Length}|DistinctForces={forceCount}|DistinctSignedStatuses={statusCount}");
            throw new InvalidOperationException(
                $"BC-AUD-007 failing-first: {completed.Length} duplicate-depth permutations created completed calculation/provenance authority; distinct forces={forceCount}; distinct signed statuses={statusCount}.");
        }

        if (outcomes.Any(x => x.Failure is null || !x.Failure.Message.Contains(BlockingMarker, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException(
                "BC-AUD-007: duplicate-depth request was not rejected by the stable production validation contract.");
        }

        Console.WriteLine(
            "BC_AUD_007_DUPLICATE_DEPTH_PRECORE_ROLLUP|Permutations=6|CompletedRuns=0|ProvenanceCreated=False");
        Console.WriteLine("BC_AUD_007_DUPLICATE_DEPTH_PRECORE_END");
    }

    private static Outcome Execute(string name, IReadOnlyList<CurrentProfilePointInput> points)
    {
        try
        {
            return new Outcome(name, Run(points), null);
        }
        catch (Exception ex)
        {
            return new Outcome(name, null, ex);
        }
    }

    private static ApplicationCalculationRun Run(IReadOnlyList<CurrentProfilePointInput> points)
    {
        var environment = new EnvironmentInput(
            1025,
            20,
            0.8,
            0.2,
            6,
            SeabedCatalog.ById("sand"),
            true,
            points);
        var rope = new RopePreset(
            "audit:rope",
            "Audit rope",
            "Synthetic",
            14,
            70,
            0.15,
            1.0,
            "BC-AUD-007");
        var assembly = new[]
        {
            new AssemblyItemInput(AssemblyItemKind.Line, "Audit line", true, rope, null, 20.2, 1, 0, 0, 0, 0)
        };

        return ApplicationCalculationRunner.Run(
            environment,
            new BuoyInput("Audit buoy", 1.0, 100, 0.10, 0.8),
            assembly,
            new AnchorInput("Audit block", "Deadweight", "Concrete", 3000, 1.2, 1.0),
            3);
    }

    private static IReadOnlyList<Permutation> DuplicatePermutations() =>
        new[]
        {
            new Permutation("A,B,C", new[] { A, B, C }),
            new Permutation("B,A,C", new[] { B, A, C }),
            new Permutation("C,A,B", new[] { C, A, B }),
            new Permutation("C,B,A", new[] { C, B, A }),
            new Permutation("A,C,B", new[] { A, C, B }),
            new Permutation("B,C,A", new[] { B, C, A })
        };

    private static string F(double value) => value.ToString("R", CultureInfo.InvariantCulture);

    private sealed record Permutation(string Name, IReadOnlyList<CurrentProfilePointInput> Points);
    private sealed record Outcome(string Name, ApplicationCalculationRun? CompletedRun, Exception? Failure);
}
