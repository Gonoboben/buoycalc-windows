using System.Collections;
using System.Globalization;
using System.Reflection;
using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal static class SignedCandidateSelectedAuthoritySwitchRegression
{
    private static readonly Dictionary<string, MooringSignedCandidateStatus> ExpectedStatus =
        new(StringComparer.Ordinal)
        {
            ["uniform-current-slack-line"] = MooringSignedCandidateStatus.Accepted,
            ["discrete-payload"] = MooringSignedCandidateStatus.Accepted,
            ["vertical-zero-current"] = MooringSignedCandidateStatus.Indeterminate,
            ["buoyant-line"] = MooringSignedCandidateStatus.RejectedPhysical,
            ["depth-varying-current-profile"] = MooringSignedCandidateStatus.RejectedPhysical
        };

    public static void Validate()
    {
        var scenarioBuilder = typeof(HistoricalGoldenImpactRegression).GetMethod(
            "BuildHistoricalScenarios",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException(
                "Selected authority switch: HistoricalGoldenImpactRegression.BuildHistoricalScenarios was not found.");

        var definitions = scenarioBuilder.Invoke(null, null) as IEnumerable
            ?? throw new InvalidOperationException(
                "Selected authority switch: historical fixture definitions are unavailable.");

        var total = 0;
        var switched = 0;
        var preserved = 0;
        var accepted = 0;
        var rejectedPhysical = 0;
        var indeterminate = 0;

        Console.WriteLine("SIGNED_SELECTED_AUTHORITY_SWITCH_BEGIN");

        foreach (var definition in definitions.Cast<object>())
        {
            var name = RequireProperty<string>(definition, "Name");
            if (!ExpectedStatus.TryGetValue(name, out var expectedStatus))
                throw new InvalidOperationException($"Selected authority switch: unexpected fixture '{name}'.");

            total++;
            var environment = RequireProperty<EnvironmentInput>(definition, "Environment");
            var buoy = RequireProperty<BuoyInput>(definition, "Buoy");
            var assembly = RequireProperty<IReadOnlyList<AssemblyItemInput>>(definition, "Assembly");
            var anchor = RequireProperty<AnchorInput>(definition, "Anchor");
            var safetyFactor = RequireProperty<double>(definition, "SafetyFactor");

            var run = ApplicationCalculationRunner.Run(
                environment,
                buoy,
                assembly,
                anchor,
                safetyFactor);
            var snapshot = run.Snapshot;
            var selected = snapshot.SelectedShape
                ?? throw new InvalidOperationException($"Selected authority switch {name}: SelectedShape is null.");
            var candidate = snapshot.SignedCandidate
                ?? throw new InvalidOperationException($"Selected authority switch {name}: SignedCandidate is null.");
            var selectedCore = snapshot.ShadowSelectedCore
                ?? throw new InvalidOperationException($"Selected authority switch {name}: selected core is null.");
            var legacy = SelectedMooringShapeProvider.Build(
                snapshot.TechnicalReportData.Shape,
                snapshot.TechnicalReportData.IterativeSolver);

            if (candidate.Status != expectedStatus)
            {
                throw new InvalidOperationException(
                    $"Selected authority switch {name}: expected {expectedStatus}, got {candidate.Status}.");
            }

            var isAccepted = candidate.Status == MooringSignedCandidateStatus.Accepted;
            if (isAccepted)
            {
                accepted++;
                switched++;
                if (candidate.Shape is null ||
                    selectedCore.SourceIdentity != MooringShapeSourceIdentity.SignedBoundaryFeedback ||
                    !selectedCore.SelectedConverged ||
                    selectedCore.SelectedUsesDiscreteLoads != candidate.ContainsDiscreteLoads ||
                    !ReferenceEquals(selectedCore.Shape, candidate.Shape) ||
                    !ReferenceEquals(selected.Shape, candidate.Shape) ||
                    !string.Equals(selected.Source, MooringShapeSourceIdentity.SignedBoundaryFeedback.ToString(), StringComparison.Ordinal) ||
                    selected.UsesDiscreteLoads != candidate.ContainsDiscreteLoads ||
                    selected.HasGateSelection ||
                    selected.GateDecision is not null)
                {
                    throw new InvalidOperationException(
                        $"Selected authority switch {name}: Accepted signed source was not projected truthfully into SelectedShapeReadModel.");
                }
            }
            else
            {
                preserved++;
                if (candidate.Status == MooringSignedCandidateStatus.RejectedPhysical)
                    rejectedPhysical++;
                else if (candidate.Status == MooringSignedCandidateStatus.Indeterminate)
                    indeterminate++;
                else
                    throw new InvalidOperationException(
                        $"Selected authority switch {name}: unexpected non-Accepted status {candidate.Status}.");

                AssertReadModelEquivalent(name + " preserved legacy", legacy, selected);
            }

            var oldX = legacy.Shape.HorizontalOffsetM;
            var oldZ = AnchorDepth(legacy.Shape);
            var newX = selected.Shape.HorizontalOffsetM;
            var newZ = AnchorDepth(selected.Shape);

            Console.WriteLine(string.Join("|",
                "SIGNED_SELECTED_AUTHORITY_SWITCH",
                name,
                $"CandidateStatus={candidate.Status}",
                $"OldSource={legacy.Source}",
                $"NewSource={selected.Source}",
                $"OldX={Format(oldX)}",
                $"NewX={Format(newX)}",
                $"OldZ={Format(oldZ)}",
                $"NewZ={Format(newZ)}",
                $"UsesDiscreteLoads={selected.UsesDiscreteLoads}",
                $"Switched={isAccepted}",
                "DownstreamScalarAuthority=LegacyUnchanged"));
        }

        if (total != 5 || switched != 2 || preserved != 3 ||
            accepted != 2 || rejectedPhysical != 2 || indeterminate != 1)
        {
            throw new InvalidOperationException(
                $"Selected authority switch truth table mismatch: total={total}, switched={switched}, preserved={preserved}, accepted={accepted}, rejectedPhysical={rejectedPhysical}, indeterminate={indeterminate}.");
        }

        Console.WriteLine(string.Join("|",
            "SIGNED_SELECTED_AUTHORITY_SWITCH_ROLLUP",
            $"Scenarios={total}",
            $"Switched={switched}",
            $"Preserved={preserved}",
            $"Accepted={accepted}",
            $"RejectedPhysical={rejectedPhysical}",
            $"Indeterminate={indeterminate}",
            "DownstreamScalarAuthority=LegacyUnchanged"));
        Console.WriteLine("SIGNED_SELECTED_AUTHORITY_SWITCH_END");
    }

    private static double AnchorDepth(MooringShapeResult shape)
    {
        if (shape.AnchorPoint is not null)
            return shape.AnchorPoint.ZDepthM;
        if (shape.Nodes.Count == 0)
            return 0.0;
        return shape.Nodes.OrderBy(x => x.Number).Last().ZDepthM;
    }

    private static string Format(double value) =>
        value.ToString("R", CultureInfo.InvariantCulture);

    private static void AssertReadModelEquivalent(
        string name,
        SelectedShapeReadModel expected,
        SelectedShapeReadModel actual)
    {
        if (!string.Equals(expected.Source, actual.Source, StringComparison.Ordinal) ||
            expected.UsesDiscreteLoads != actual.UsesDiscreteLoads ||
            expected.HasGateSelection != actual.HasGateSelection ||
            expected.GateDecision != actual.GateDecision ||
            !string.Equals(expected.DecisionText, actual.DecisionText, StringComparison.Ordinal) ||
            !string.Equals(expected.MethodNote, actual.MethodNote, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"{name}: selected read-model metadata changed.");
        }

        AssertShapeEquivalent(name, expected.Shape, actual.Shape);
    }

    private static void AssertShapeEquivalent(string name, MooringShapeResult expected, MooringShapeResult actual)
    {
        if (expected.BuoyState != actual.BuoyState ||
            expected.DepthM != actual.DepthM ||
            expected.LineLengthM != actual.LineLengthM ||
            expected.HorizontalOffsetM != actual.HorizontalOffsetM ||
            expected.VerticalResidualM != actual.VerticalResidualM ||
            expected.Converged != actual.Converged ||
            expected.MethodNote != actual.MethodNote ||
            expected.IterationCount != actual.IterationCount ||
            expected.ConvergenceResidualM != actual.ConvergenceResidualM ||
            expected.AngleScale != actual.AngleScale ||
            expected.ConvergenceCriterion != actual.ConvergenceCriterion ||
            expected.Nodes.Count != actual.Nodes.Count)
        {
            throw new InvalidOperationException($"{name}: selected shape scalar/metadata state changed.");
        }

        for (var i = 0; i < expected.Nodes.Count; i++)
        {
            var a = expected.Nodes[i];
            var b = actual.Nodes[i];
            if (a.Number != b.Number ||
                a.SegmentNumber != b.SegmentNumber ||
                a.Label != b.Label ||
                a.AlongLineM != b.AlongLineM ||
                a.XOffsetM != b.XOffsetM ||
                a.ZDepthM != b.ZDepthM ||
                a.SegmentLengthM != b.SegmentLengthM ||
                a.SegmentAngleFromVerticalDeg != b.SegmentAngleFromVerticalDeg ||
                a.SegmentTensionKn != b.SegmentTensionKn ||
                a.Status != b.Status)
            {
                throw new InvalidOperationException($"{name}: selected node {i} changed.");
            }
        }
    }

    private static T RequireProperty<T>(object source, string propertyName)
    {
        var property = source.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                $"Selected authority switch: property {source.GetType().Name}.{propertyName} was not found.");
        var value = property.GetValue(source);
        if (value is T typed)
            return typed;
        throw new InvalidOperationException(
            $"Selected authority switch: property {source.GetType().Name}.{propertyName} is not {typeof(T).Name}.");
    }
}
