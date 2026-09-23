using System.Globalization;
using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal static class OneRunAllArtifactsSelectedAuthorityExactlyEqualRegression
{
    private const string SelectedGeometryHeading = "## Выбранная инженерная геометрия X/Z";

    public static void OneRun_AllArtifacts_SelectedAuthorityExactlyEqual()
    {
        Console.WriteLine("BC_AUD_001_ONE_RUN_ALL_ARTIFACTS_SELECTED_AUTHORITY_BEGIN");

        foreach (var lineLengthM in new[] { 20.2, 20.3 })
            ValidateScenario(lineLengthM);

        Console.WriteLine(
            "BC_AUD_001_ONE_RUN_ALL_ARTIFACTS_SELECTED_AUTHORITY_ROLLUP|Scenarios=2|LineLengthsM=20.2,20.3|SnapshotEqualsTypedReport=True|SnapshotEqualsPdfDiagram=True|SnapshotEqualsFullTxt=True|EveryNodeCompared=True");
        Console.WriteLine("BC_AUD_001_ONE_RUN_ALL_ARTIFACTS_SELECTED_AUTHORITY_END");
    }

    private static void ValidateScenario(double lineLengthM)
    {
        var rope = new RopePreset(
            "audit:rope",
            "Audit rope 14 mm",
            "Synthetic",
            14,
            70,
            0.15,
            1.0,
            "BC-AUD-001 regression");
        var connector = new ConnectorPreset(
            "audit:connector",
            "Audit connector",
            "Shackle",
            1.2,
            0.00008,
            55,
            0.004,
            1.2,
            "BC-AUD-001 regression");
        var buoy = new BuoyInput("Audit buoy", 1.0, 100, 0.10, 0.8);
        var anchor = new AnchorInput("Audit block", "Deadweight", "Concrete", 3000, 1.2, 1.0);
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
        var assembly = new[]
        {
            new AssemblyItemInput(AssemblyItemKind.Connector, "Top connector", true, null, connector, 0, 1, 0, 0, 0, 0),
            new AssemblyItemInput(AssemblyItemKind.Line, "Audit line", true, rope, null, lineLengthM, 1, 0, 0, 0, 0),
            new AssemblyItemInput(AssemblyItemKind.Connector, "Bottom connector", true, null, connector, 0, 1, 0, 0, 0, 0)
        };

        var run = ApplicationCalculationRunner.Run(environment, buoy, assembly, anchor, 3);
        var snapshotSelected = run.Snapshot.SelectedShape
            ?? throw new InvalidOperationException($"BC-AUD-001 {F(lineLengthM)} m: Accepted run did not expose SelectedShape.");
        if (run.Snapshot.SignedCandidate?.Status != MooringSignedCandidateStatus.Accepted)
            throw new InvalidOperationException($"BC-AUD-001 {F(lineLengthM)} m: signed candidate is not Accepted.");

        var projectName = $"BC-AUD-001-{F(lineLengthM)}";
        var typedReport = UserEngineeringReportReadModelProjector.Project(
            projectName,
            environment,
            buoy,
            anchor,
            run.Snapshot);
        var typedSelected = typedReport.SelectedShape
            ?? throw new InvalidOperationException($"BC-AUD-001 {F(lineLengthM)} m: typed report did not expose selected geometry.");
        var pdfDiagram = Mooring2DDiagramReadModelBuilder.Build(
            typedSelected,
            SelectedElementCalculationDisplayProjector.Project(run.Snapshot));

        AssertSameSelectedShape(snapshotSelected, typedSelected, lineLengthM, "typed report");
        AssertSameSelectedShape(snapshotSelected, pdfDiagram.SelectedShape, lineLengthM, "PDF diagram read model");
        if (!ReferenceEquals(snapshotSelected, typedSelected) ||
            !ReferenceEquals(typedSelected, pdfDiagram.SelectedShape))
        {
            throw new InvalidOperationException(
                $"BC-AUD-001 {F(lineLengthM)} m: typed report/PDF diagram did not retain the snapshot selected authority instance.");
        }

        var fullText = TechnicalReportBuilder.Build(projectName, environment, buoy, anchor, run.Snapshot);
        var fullTextSelected = ParseSelectedGeometry(fullText, lineLengthM);

        Exact(fullTextSelected.HorizontalOffsetM, snapshotSelected.Shape.HorizontalOffsetM, lineLengthM, "Full TXT horizontal offset");
        if (fullTextSelected.Nodes.Count != snapshotSelected.Shape.Nodes.Count)
        {
            throw new InvalidOperationException(
                $"BC-AUD-001 {F(lineLengthM)} m: Full TXT selected node count {fullTextSelected.Nodes.Count} != snapshot {snapshotSelected.Shape.Nodes.Count}.");
        }

        for (var i = 0; i < snapshotSelected.Shape.Nodes.Count; i++)
        {
            var expected = snapshotSelected.Shape.Nodes[i];
            var actual = fullTextSelected.Nodes[i];
            if (actual.Number != expected.Number)
                throw new InvalidOperationException($"BC-AUD-001 {F(lineLengthM)} m: Full TXT node #{i} identity changed.");
            Exact(actual.XOffsetM, expected.XOffsetM, lineLengthM, $"Full TXT node #{expected.Number} X");
            Exact(actual.ZDepthM, expected.ZDepthM, lineLengthM, $"Full TXT node #{expected.Number} Z");
        }

        if (fullText.Contains("- Снос формы X/Z:", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"BC-AUD-001 {F(lineLengthM)} m: neutral fallback X/Z offset remains in Accepted Full TXT.");
        }

        Console.WriteLine(string.Join(
            "|",
            "BC_AUD_001_ONE_RUN_ALL_ARTIFACTS_SELECTED_AUTHORITY",
            $"LineLengthM={F(lineLengthM)}",
            $"SelectedX={F(snapshotSelected.Shape.HorizontalOffsetM)}",
            $"Nodes={snapshotSelected.Shape.Nodes.Count}",
            "SnapshotEqualsTypedReport=True",
            "SnapshotEqualsPdfDiagram=True",
            "SnapshotEqualsFullTxt=True"));
    }

    private static void AssertSameSelectedShape(
        SelectedShapeReadModel expected,
        SelectedShapeReadModel actual,
        double lineLengthM,
        string consumer)
    {
        if (actual.Source != expected.Source ||
            actual.UsesDiscreteLoads != expected.UsesDiscreteLoads ||
            actual.HasGateSelection != expected.HasGateSelection ||
            actual.GateDecision != expected.GateDecision ||
            actual.Shape.Converged != expected.Shape.Converged ||
            actual.Shape.Nodes.Count != expected.Shape.Nodes.Count)
        {
            throw new InvalidOperationException(
                $"BC-AUD-001 {F(lineLengthM)} m: {consumer} selected geometry metadata differs from snapshot.");
        }

        Exact(actual.Shape.HorizontalOffsetM, expected.Shape.HorizontalOffsetM, lineLengthM, consumer + " horizontal offset");
        for (var i = 0; i < expected.Shape.Nodes.Count; i++)
        {
            var expectedNode = expected.Shape.Nodes[i];
            var actualNode = actual.Shape.Nodes[i];
            Exact(actualNode.XOffsetM, expectedNode.XOffsetM, lineLengthM, $"{consumer} node #{expectedNode.Number} X");
            Exact(actualNode.ZDepthM, expectedNode.ZDepthM, lineLengthM, $"{consumer} node #{expectedNode.Number} Z");
        }
    }

    private static ParsedSelectedGeometry ParseSelectedGeometry(string report, double lineLengthM)
    {
        var lines = report.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var headingIndex = Array.FindIndex(lines, line => line == SelectedGeometryHeading);
        if (headingIndex < 0)
        {
            throw new InvalidOperationException(
                $"BC-AUD-001 {F(lineLengthM)} m: authoritative selected X/Z section is missing from Full TXT.");
        }

        var endIndex = headingIndex + 1;
        while (endIndex < lines.Length && !lines[endIndex].StartsWith("## ", StringComparison.Ordinal))
            endIndex++;

        const string offsetPrefix = "- Authoritative horizontal offset X, m: ";
        var offsetLine = lines[headingIndex..endIndex]
            .SingleOrDefault(line => line.StartsWith(offsetPrefix, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"BC-AUD-001 {F(lineLengthM)} m: authoritative Full TXT X offset is missing.");
        var horizontalOffsetM = double.Parse(offsetLine[offsetPrefix.Length..], CultureInfo.InvariantCulture);

        var nodes = new List<ParsedNode>();
        foreach (var line in lines[(headingIndex + 1)..endIndex])
        {
            if (!line.StartsWith("| ", StringComparison.Ordinal))
                continue;

            var cells = line.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (cells.Length != 3 || !int.TryParse(cells[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
                continue;

            nodes.Add(new ParsedNode(
                number,
                double.Parse(cells[1], CultureInfo.InvariantCulture),
                double.Parse(cells[2], CultureInfo.InvariantCulture)));
        }

        return new ParsedSelectedGeometry(horizontalOffsetM, nodes);
    }

    private static void Exact(double actual, double expected, double lineLengthM, string label)
    {
        if (actual != expected)
        {
            throw new InvalidOperationException(
                $"BC-AUD-001 {F(lineLengthM)} m: {label} expected exact {F(expected)}, got {F(actual)}.");
        }
    }

    private static string F(double value) => value.ToString("R", CultureInfo.InvariantCulture);

    private sealed record ParsedSelectedGeometry(double HorizontalOffsetM, IReadOnlyList<ParsedNode> Nodes);
    private sealed record ParsedNode(int Number, double XOffsetM, double ZDepthM);
}
