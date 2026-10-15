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
        Console.WriteLine("BC_AUD_008_DIRECTION_AUTHORITY|EastNorthMagnitudeInvariance=True|SignedProfileRetained=True|AcceptedSelectedXZRequired=True|F1F2F3F4Equal=True|UIAzimuthPassive=True|FullTxtDisclosure=True|PdfDisclosure=True|SchemaUnchanged=True");
        Console.WriteLine("BC_AUD_008_DIRECTION_AUTHORITY_END");
    }

    private static void ValidateRotatedAndReversedCurrent()
    {
        var rope = new RopePreset("bc-aud-008:rope", "Audit rope 14 mm", "Synthetic", 14, 70, 0.15, 1.0, "");
        var connector = new ConnectorPreset("bc-aud-008:connector", "Audit connector", "Shackle", 1.2, 0.00008, 55, 0.004, 1.2, "");
        var buoy = new BuoyInput("Audit buoy", 1.0, 100, 0.1, 0.8);
        var anchor = new AnchorInput("Audit anchor", "Deadweight", "Concrete", 3000, 1.2, 1.0);
        // Copy the independently proven Accepted scenario from BC-AUD-001.
        // A null selected shape or any missing F1-F4 is now an explicit failure.
        var assembly = new[]
        {
            new AssemblyItemInput(AssemblyItemKind.Connector, "Top connector", true, null, connector, 0, 1, 0, 0, 0, 0),
            new AssemblyItemInput(AssemblyItemKind.Line, "Audit line", true, rope, null, 20.2, 1, 0, 0, 0, 0),
            new AssemblyItemInput(AssemblyItemKind.Connector, "Bottom connector", true, null, connector, 0, 1, 0, 0, 0, 0)
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

        RequireAcceptedSelectedAuthorities(east, "east-only");
        RequireAcceptedSelectedAuthorities(north, "north-only");
        RequireAcceptedSelectedAuthorities(west, "west-only");

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

            var aShape = east.Snapshot.SelectedShape!.Shape;
            var bShape = comparison.Snapshot.SelectedShape!.Shape;
            if (aShape.Nodes.Count < 2 || bShape.Nodes.Count != aShape.Nodes.Count)
                throw new InvalidOperationException("BC-AUD-008: accepted selected X/Z node count changed: " + label);
            Exact(aShape.HorizontalOffsetM, bShape.HorizontalOffsetM, label + " selected horizontal offset");
            for (var i = 0; i < aShape.Nodes.Count; i++)
            {
                Exact(aShape.Nodes[i].XOffsetM, bShape.Nodes[i].XOffsetM, label + " selected X");
                Exact(aShape.Nodes[i].ZDepthM, bShape.Nodes[i].ZDepthM, label + " selected Z");
            }

            RequireSameSelectedAuthorities(east.Snapshot, comparison.Snapshot, label);
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
        // Retain the exact checked PDF in a CI artifact for visual review.
        // The uploader must fail the workflow if the file is missing.
        var evidenceDir = Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "bc-aud-008");
        Directory.CreateDirectory(evidenceDir);
        var pdf = Path.Combine(evidenceDir, "bc-aud-008-typed-user-report.pdf");
        PdfReportBuilder.Build(pdf, report);
        using var document = PdfDocument.Open(pdf);
        var pages = document.GetPages().ToArray();
        if (pages.Length < 2)
            throw new InvalidOperationException("BC-AUD-008: PDF lacks expected conditions page.");
        foreach (var (page, pageIndex) in pages.Select((page, index) => (page, index)))
        {
            if (Math.Abs(page.Width - 595) > 1 || Math.Abs(page.Height - 842) > 1)
                throw new InvalidOperationException($"BC-AUD-008: page {pageIndex + 1} is not A4.");
            foreach (var letter in page.Letters)
            {
                var box = letter.GlyphRectangle;
                if (box.Left < -1 || box.Right > page.Width + 1 || box.Bottom < -1 || box.Top > page.Height + 1)
                    throw new InvalidOperationException($"BC-AUD-008: glyph outside page {pageIndex + 1}.");
            }
        }
        var pageTexts = pages.Select(page => string.Concat(page.Letters.Select(letter => letter.Value))).ToArray();
        Require(pageTexts[0], CurrentDirectionModelDisclosure.Notice, "PDF executive notice");
        Require(pageTexts[1], CurrentDirectionModelDisclosure.Detail, "PDF conditions limitation");
        Console.WriteLine($"BC_AUD_008_PDF_ARTIFACT|Pages={pages.Length}|A4=True|GlyphsInsidePage=True|Notice=True|Detail=True|Path=artifacts/bc-aud-008/bc-aud-008-typed-user-report.pdf");
    }

    private static void RequireAcceptedSelectedAuthorities(ApplicationCalculationRun run, string label)
    {
        var snapshot = run.Snapshot;
        if (snapshot.SignedCandidate?.Status != MooringSignedCandidateStatus.Accepted ||
            snapshot.SelectedShape is null || snapshot.SelectedShape.Shape.Nodes.Count < 2 ||
            snapshot.SelectedDesignTensionDemand is null || snapshot.SelectedAnchorReaction is null ||
            snapshot.SelectedLocalStructuralCapacity is null || snapshot.SelectedEngineeringAssessment is null)
            throw new InvalidOperationException("BC-AUD-008: positive control must have Accepted selected X/Z and F1-F4: " + label);

        var identity = MooringShapeSourceIdentity.SignedBoundaryFeedback;
        if (snapshot.SelectedDesignTensionDemand.SourceIdentity != identity ||
            snapshot.SelectedAnchorReaction.SourceIdentity != identity ||
            snapshot.SelectedLocalStructuralCapacity.SourceIdentity != identity ||
            snapshot.SelectedEngineeringAssessment.SourceIdentity != identity ||
            snapshot.SelectedEngineeringAssessment.IsDirectHardFailureTerminal)
            throw new InvalidOperationException("BC-AUD-008: selected F1-F4 source identity is missing or inconsistent: " + label);
    }

    private static void RequireSameSelectedAuthorities(CalculationSnapshot baseline, CalculationSnapshot candidate, string label)
    {
        var f1 = baseline.SelectedDesignTensionDemand!;
        var f1Other = candidate.SelectedDesignTensionDemand!;
        Exact(f1.DemandN, f1Other.DemandN, label + " F1 design demand");
        Exact(f1.DemandKn, f1Other.DemandKn, label + " F1 design kN");
        if (f1.LocationKind != f1Other.LocationKind || f1.SegmentNumber != f1Other.SegmentNumber)
            throw new InvalidOperationException("BC-AUD-008: F1 location changed: " + label);

        var f2 = baseline.SelectedAnchorReaction!;
        var f2Other = candidate.SelectedAnchorReaction!;
        Exact(f2.HorizontalDemandN, f2Other.HorizontalDemandN, label + " F2 horizontal demand");
        Exact(f2.SignedNormalReactionN, f2Other.SignedNormalReactionN, label + " F2 normal reaction");
        if (f2.ContactClassification != f2Other.ContactClassification)
            throw new InvalidOperationException("BC-AUD-008: F2 anchor contact changed: " + label);

        var f3 = baseline.SelectedLocalStructuralCapacity!;
        var f3Other = candidate.SelectedLocalStructuralCapacity!;
        if (f3.Rows.Count == 0 || f3.Rows.Count != f3Other.Rows.Count ||
            f3.GoverningElementNumber != f3Other.GoverningElementNumber ||
            f3.StructuralCapacityCoverageComplete != f3Other.StructuralCapacityCoverageComplete)
            throw new InvalidOperationException("BC-AUD-008: F3 element coverage changed: " + label);
        for (var i = 0; i < f3.Rows.Count; i++)
        {
            var a = f3.Rows[i];
            var b = f3Other.Rows[i];
            if (a.ElementNumber != b.ElementNumber || a.Status != b.Status ||
                a.LocalDesignDemandN != b.LocalDesignDemandN || a.LocalReserve != b.LocalReserve)
                throw new InvalidOperationException("BC-AUD-008: F3 selected local structural row changed: " + label);
        }

        var f4 = baseline.SelectedEngineeringAssessment!;
        var f4Other = candidate.SelectedEngineeringAssessment!;
        if (f4.Verdict != f4Other.Verdict || f4.MainRiskCode != f4Other.MainRiskCode ||
            f4.HasHardFailure != f4Other.HasHardFailure ||
            f4.RequiresReview != f4Other.RequiresReview ||
            f4.Checks.Count == 0 || f4.Checks.Count != f4Other.Checks.Count)
            throw new InvalidOperationException("BC-AUD-008: F4 selected verdict/check count changed: " + label);
        for (var i = 0; i < f4.Checks.Count; i++)
        {
            var a = f4.Checks[i];
            var b = f4Other.Checks[i];
            if (a.Kind != b.Kind || a.Status != b.Status || a.Code != b.Code)
                throw new InvalidOperationException("BC-AUD-008: F4 selected assessment check changed: " + label);
        }
    }

    private static void ValidatePassiveAzimuthUiPath()
    {
        var vm = new MainWindowViewModel();
        // A fresh UI project need not have a validated profile yet; supplying explicit
        // depth points is a prerequisite, not an optional scalar-current fallback.
        vm.CurrentProfilePoints.Clear();
        vm.CurrentProfilePoints.Add(new CurrentProfilePointViewModel
        {
            DepthM = "0",
            EastCurrentMS = "0.2",
            NorthCurrentMS = "0",
            VerticalCurrentMS = "0",
            WaterDensityKgM3 = "1025"
        });
        vm.CurrentProfilePoints.Add(new CurrentProfilePointViewModel
        {
            DepthM = "50",
            EastCurrentMS = "0.2",
            NorthCurrentMS = "0",
            VerticalCurrentMS = "0",
            WaterDensityKgM3 = "1025"
        });
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
