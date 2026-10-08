using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;
using BuoyCalc.Windows.ViewModels;
using UglyToad.PdfPig;

internal static class AxisAzimuth_HasDefinedEffectOrIsNotAnInputRegression
{
    public static void Validate()
    {
        Console.WriteLine("BC_AUD_008_DIRECTION_AUTHORITY_BEGIN");
        ValidateRotatedAndReversedCurrent();
        ValidatePassiveAzimuthUiPath();
        Console.WriteLine("BC_AUD_008_DIRECTION_AUTHORITY|EastNorthMagnitudeInvariance=True|SignedProfileRetained=True|SelectedXZInvariant=True|UIAzimuthPassive=True|FullTxtDisclosure=True|PdfDisclosure=True|SchemaUnchanged=True");
        Console.WriteLine("BC_AUD_008_DIRECTION_AUTHORITY_END");
    }

    private static void ValidateRotatedAndReversedCurrent()
    {
        var rope = new RopePreset("bc-aud-008:rope", "Audit rope 14 mm", "Synthetic", 14, 70, 0.15, 1.0, "");
        var buoy = new BuoyInput("Audit buoy", 1.0, 100, 0.1, 0.8);
        var anchor = new AnchorInput("Audit anchor", "Deadweight", "Concrete", 3000, 1.2, 1.0);
        var assembly = new[]
        {
            new AssemblyItemInput(AssemblyItemKind.Line, "Audit line", true, rope, null, 20.2, 1, 0, 0, 0, 0)
        };

        EnvironmentInput Environment(double east, double north) => new(
            1025, 20, 0.2, 0.2, 6, SeabedCatalog.ById("sand"), true,
            new[]
            {
                new CurrentProfilePointInput(0, east, north, 0, 1025),
                new CurrentProfilePointInput(20, east, north, 0, 1025)
            });

        var east = ApplicationCalculationRunner.Run(Environment(0.2, 0), buoy, assembly, anchor, 3);
        var north = ApplicationCalculationRunner.Run(Environment(0, 0.2), buoy, assembly, anchor, 3);
        var west = ApplicationCalculationRunner.Run(Environment(-0.2, 0), buoy, assembly, anchor, 3);

        foreach (var (label, comparison) in new[] { ("north-only", north), ("west-only", west) })
        {
            Exact(east.Result.CurrentForceN, comparison.Result.CurrentForceN, label + " total current load");
            Exact(east.Result.TensionKn, comparison.Result.TensionKn, label + " tension");
            Exact(east.Result.AnchorReserve, comparison.Result.AnchorReserve, label + " anchor reserve");
            if (east.Result.Verdict != comparison.Result.Verdict)
                throw new InvalidOperationException("BC-AUD-008: magnitude-identical directions changed verdict: " + label);
            if (east.Result.SegmentRows.Count != comparison.Result.SegmentRows.Count)
                throw new InvalidOperationException("BC-AUD-008: rotation changed segment count: " + label);
            for (var i = 0; i < east.Result.SegmentRows.Count; i++)
            {
                var a = east.Result.SegmentRows[i];
                var b = comparison.Result.SegmentRows[i];
                Exact(a.LocalSpeedMS, b.LocalSpeedMS, label + " segment speed");
                Exact(a.CurrentForceN, b.CurrentForceN, label + " segment load");
            }

            var aShape = east.Snapshot.SelectedShape?.Shape;
            var bShape = comparison.Snapshot.SelectedShape?.Shape;
            if ((aShape is null) != (bShape is null))
                throw new InvalidOperationException("BC-AUD-008: selected X/Z availability changed: " + label);
            if (aShape is not null && bShape is not null)
            {
                if (aShape.Nodes.Count != bShape.Nodes.Count)
                    throw new InvalidOperationException("BC-AUD-008: selected node count changed: " + label);
                for (var i = 0; i < aShape.Nodes.Count; i++)
                {
                    Exact(aShape.Nodes[i].XOffsetM, bShape.Nodes[i].XOffsetM, label + " selected X");
                    Exact(aShape.Nodes[i].ZDepthM, bShape.Nodes[i].ZDepthM, label + " selected Z");
                }
            }
        }

        var eastComponent = east.Result.SegmentRows[0];
        var northComponent = north.Result.SegmentRows[0];
        var westComponent = west.Result.SegmentRows[0];
        if (eastComponent.EastCurrentMS <= 0 || eastComponent.NorthCurrentMS != 0 ||
            northComponent.NorthCurrentMS <= 0 || northComponent.EastCurrentMS != 0 ||
            westComponent.EastCurrentMS >= 0)
            throw new InvalidOperationException("BC-AUD-008: signed East/North input data was not retained.");

        if (east.Snapshot.Provenance.InputHash == north.Snapshot.Provenance.InputHash ||
            east.Snapshot.Provenance.InputHash == west.Snapshot.Provenance.InputHash)
            throw new InvalidOperationException("BC-AUD-008: distinct signed canonical inputs must have distinct InputHash.");

        var profile = Environment(0.2, 0).EffectiveCurrentProfile;
        if (ProfilePlanarProjectionReadModelBuilder.Build(profile, 0).Rows[0].UXMS ==
            ProfilePlanarProjectionReadModelBuilder.Build(profile, 90).Rows[0].UXMS)
            throw new InvalidOperationException("BC-AUD-008: diagnostic INFO projection must retain azimuth dependence.");

        var text = TechnicalReportBuilder.Build("BC-AUD-008", Environment(0.2, 0), buoy, anchor, east.Snapshot);
        Require(text, CurrentDirectionModelDisclosure.Notice, "Full TXT notice");
        Require(text, CurrentDirectionModelDisclosure.Detail, "Full TXT limitation");

        var report = UserEngineeringReportReadModelProjector.Project(
            "BC-AUD-008", Environment(0.2, 0), buoy, anchor, east.Snapshot);
        var pdf = Path.Combine(Path.GetTempPath(), "bc-aud-008-" + Guid.NewGuid().ToString("N") + ".pdf");
        try
        {
            PdfReportBuilder.Build(pdf, report);
            using var document = PdfDocument.Open(pdf);
            var pageTexts = document.GetPages()
                .Select(page => string.Concat(page.Letters.Select(letter => letter.Value)))
                .ToArray();
            if (pageTexts.Length < 2)
                throw new InvalidOperationException("BC-AUD-008: PDF lacks expected conditions page.");
            Require(pageTexts[0], CurrentDirectionModelDisclosure.Notice, "PDF executive notice");
            Require(pageTexts[1], CurrentDirectionModelDisclosure.Detail, "PDF conditions limitation");
        }
        finally
        {
            if (File.Exists(pdf)) File.Delete(pdf);
        }
    }

    private static void ValidatePassiveAzimuthUiPath()
    {
        var vm = new MainWindowViewModel();
        vm.PlanarXAxisAzimuthDeg = "0";
        vm.CalculateCommand.Execute(null);
        if (!vm.IsCalculationCurrent || vm.UserEngineeringReport is null)
            throw new InvalidOperationException("BC-AUD-008: default project calculation did not complete.");

        var first = vm.UserEngineeringReport.Provenance;
        vm.PlanarXAxisAzimuthDeg = "90";
        if (vm.IsCalculationCurrent || vm.CanExportPdf || vm.CanExportFullReport)
            throw new InvalidOperationException("BC-AUD-008: edited passive azimuth did not invalidate old exports.");

        vm.CalculateCommand.Execute(null);
        if (!vm.IsCalculationCurrent || vm.UserEngineeringReport is null)
            throw new InvalidOperationException("BC-AUD-008: recalculation after azimuth edit did not complete.");

        var second = vm.UserEngineeringReport.Provenance;
        if (first.InputHash != second.InputHash || first.ResultHash != second.ResultHash)
            throw new InvalidOperationException("BC-AUD-008: diagnostic-only azimuth altered canonical inputs/result fingerprint.");
        if (CalculationRunFingerprint.InputSchema != "buoycalc-engineering-input/v1" ||
            CalculationRunFingerprint.ResultSchema != "buoycalc-engineering-result/v3")
            throw new InvalidOperationException("BC-AUD-008: approved fingerprint schemas changed.");
        Require(vm.ReportText, CurrentDirectionModelDisclosure.Notice, "VM Full TXT notice");
    }

    private static void Exact(double expected, double actual, string label)
    {
        if (expected != actual)
            throw new InvalidOperationException($"BC-AUD-008 {label}: expected {expected:R}, got {actual:R}.");
    }

    private static void Require(string output, string phrase, string label)
    {
        static string Normalized(string value) => string.Concat(value.Where(character => !char.IsWhiteSpace(character)));
        if (!Normalized(output).Contains(Normalized(phrase), StringComparison.Ordinal))
            throw new InvalidOperationException($"BC-AUD-008 {label}: disclosure absent from published output.");
    }
}
