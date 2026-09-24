using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal static class RejectedShortLineReportIsLocalizedActionableAndLoadSafeRegression
{
    public static void RejectedShortLine_ReportIsLocalizedActionableAndLoadSafe()
    {
        var environment = new EnvironmentInput(
            1025,
            85,
            0.2,
            0.5,
            6,
            SeabedCatalog.ById("sand"),
            true,
            new[]
            {
                new CurrentProfilePointInput(0, 0.2, 0, 0, 1025),
                new CurrentProfilePointInput(85, 0.2, 0, 0, 1025)
            });
        var buoy = new BuoyInput("BC-AUD-009 buoy", 1, 100, 0.1, 0.8);
        var anchor = new AnchorInput("BC-AUD-009 anchor", "Deadweight", "Concrete", 3000, 1.2, 1);
        var rope = new RopePreset(
            "audit:bc-aud-009-rope",
            "BC-AUD-009 rope",
            "Synthetic",
            14,
            70,
            0.15,
            1,
            "BC-AUD-009 failing-first fixture");
        var assembly = new[]
        {
            new AssemblyItemInput(
                AssemblyItemKind.Line,
                "Active line",
                true,
                rope,
                null,
                60,
                1,
                0,
                0,
                0,
                0)
        };

        var run = ApplicationCalculationRunner.Run(environment, buoy, assembly, anchor, 3);
        var signed = run.Snapshot.SignedCandidate
            ?? throw new InvalidOperationException("BC-AUD-009 failing-first: signed candidate is absent.");

        if (run.Result.SegmentRows.Count != 300 ||
            signed.Status != MooringSignedCandidateStatus.RejectedPhysical)
        {
            throw new InvalidOperationException(
                $"BC-AUD-009 failing-first fixture drifted: SegmentRows={run.Result.SegmentRows.Count}; Signed={signed.Status}.");
        }

        Console.WriteLine(
            $"BC_AUD_009_FAILING_FIRST|DepthM=85|ActiveLineM=60|CoreInvoked=True|SegmentRows={run.Result.SegmentRows.Count}|Signed={signed.Status}");
        throw new InvalidOperationException(
            "BC-AUD-009 expected failure: production application path still invokes the calculation core and creates 300 segments before short-line rejection.");
    }
}
