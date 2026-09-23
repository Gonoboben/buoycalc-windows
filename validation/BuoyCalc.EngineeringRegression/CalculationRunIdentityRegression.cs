using System.Reflection;
using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal static class CalculationRunIdentityRegression
{
    public static void CalculationRunIdentity_PropagatesExactlyAcrossArtifacts()
    {
        Console.WriteLine("BC_AUD_005_CALCULATION_RUN_IDENTITY_BEGIN");

        var environment = new EnvironmentInput(
            1025,
            20,
            0.2,
            0.2,
            6,
            SeabedCatalog.ById("sand"),
            true,
            new[]
            {
                new CurrentProfilePointInput(0, 0.2, 0, 0, 1025),
                new CurrentProfilePointInput(20, 0.2, 0, 0, 1025)
            });
        var buoy = new BuoyInput("Audit buoy", 1.0, 100, 0.10, 0.8);
        var anchor = new AnchorInput("Audit block", "Deadweight", "Concrete", 3000, 1.2, 1.0);
        var rope = new RopePreset("audit:rope", "Audit rope", "Synthetic", 14, 70, 0.15, 1.0, "BC-AUD-005");
        var assembly = new[]
        {
            new AssemblyItemInput(AssemblyItemKind.Line, "Audit line", true, rope, null, 20.2, 1, 0, 0, 0, 0)
        };

        var run = ApplicationCalculationRunner.Run(environment, buoy, assembly, anchor, 3);
        var provenanceProperty = typeof(CalculationSnapshot).GetProperty(
            "Provenance",
            BindingFlags.Public | BindingFlags.Instance);

        if (provenanceProperty is null)
        {
            throw new InvalidOperationException(
                "BC-AUD-005 reproduced: CalculationSnapshot has no retained immutable Provenance authority.");
        }

        var provenance = provenanceProperty.GetValue(run.Snapshot)
            ?? throw new InvalidOperationException(
                "BC-AUD-005 reproduced: completed calculation snapshot has null Provenance authority.");

        var report = UserEngineeringReportReadModelProjector.Project(
            "BC-AUD-005",
            environment,
            buoy,
            anchor,
            run.Snapshot);
        var reportProvenance = report.GetType().GetProperty("Provenance")?.GetValue(report);
        if (!ReferenceEquals(provenance, reportProvenance))
        {
            throw new InvalidOperationException(
                "BC-AUD-005 reproduced: typed report does not retain the snapshot provenance authority.");
        }

        var fullText = TechnicalReportBuilder.Build("BC-AUD-005", environment, buoy, anchor, run.Snapshot);
        if (!fullText.Contains("## Calculation run provenance", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "BC-AUD-005 reproduced: Full TXT has no calculation-run provenance section.");
        }

        Console.WriteLine("BC_AUD_005_CALCULATION_RUN_IDENTITY_END");
    }
}
