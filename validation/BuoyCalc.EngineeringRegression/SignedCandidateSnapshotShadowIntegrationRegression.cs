using System.Collections;
using System.Reflection;
using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal static class SignedCandidateSnapshotShadowIntegrationRegression
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
                "Signed snapshot integration: HistoricalGoldenImpactRegression.BuildHistoricalScenarios was not found.");
        var definitions = scenarioBuilder.Invoke(null, null) as IEnumerable
            ?? throw new InvalidOperationException("Signed snapshot integration: historical fixtures are unavailable.");

        var total = 0;
        var accepted = 0;
        var rejectedPhysical = 0;
        var indeterminate = 0;

        foreach (var definition in definitions.Cast<object>())
        {
            var name = RequireProperty<string>(definition, "Name");
            if (!ExpectedStatus.TryGetValue(name, out var expectedStatus))
                throw new InvalidOperationException($"Signed snapshot integration: unexpected fixture '{name}'.");

            total++;
            var environment = RequireProperty<EnvironmentInput>(definition, "Environment");
            var buoy = RequireProperty<BuoyInput>(definition, "Buoy");
            var assembly = RequireProperty<IReadOnlyList<AssemblyItemInput>>(definition, "Assembly");
            var anchor = RequireProperty<AnchorInput>(definition, "Anchor");
            var safetyFactor = RequireProperty<double>(definition, "SafetyFactor");
            var run = ApplicationCalculationRunner.Run(environment, buoy, assembly, anchor, safetyFactor);
            var snapshot = run.Snapshot;
            var selected = snapshot.SelectedShape
                ?? throw new InvalidOperationException($"Signed snapshot integration {name}: SelectedShape is null.");
            var candidate = snapshot.SignedCandidate
                ?? throw new InvalidOperationException($"Signed snapshot integration {name}: SignedCandidate is null.");
            var selectedCore = snapshot.ShadowSelectedCore
                ?? throw new InvalidOperationException($"Signed snapshot integration {name}: selected core is null.");
            var legacy = SelectedMooringShapeProvider.Build(
                snapshot.TechnicalReportData.Shape,
                snapshot.TechnicalReportData.IterativeSolver);

            if (candidate.Status != expectedStatus)
                throw new InvalidOperationException($"Signed snapshot integration {name}: expected {expectedStatus}, got {candidate.Status}.");

            if (candidate.Status == MooringSignedCandidateStatus.Accepted)
            {
                accepted++;
                if (candidate.Shape is null ||
                    selectedCore.SourceIdentity != MooringShapeSourceIdentity.SignedBoundaryFeedback ||
                    !selectedCore.SelectedConverged ||
                    selectedCore.SelectedUsesDiscreteLoads != candidate.ContainsDiscreteLoads ||
                    !ReferenceEquals(selectedCore.Shape, candidate.Shape) ||
                    !ReferenceEquals(selected.Shape, candidate.Shape) ||
                    selected.Source != MooringShapeSourceIdentity.SignedBoundaryFeedback.ToString() ||
                    selected.UsesDiscreteLoads != candidate.ContainsDiscreteLoads ||
                    selected.HasGateSelection || selected.GateDecision is not null)
                {
                    throw new InvalidOperationException(
                        $"Signed snapshot integration {name}: Accepted candidate is not represented truthfully by typed core/read model.");
                }
            }
            else
            {
                if (candidate.Status == MooringSignedCandidateStatus.RejectedPhysical)
                    rejectedPhysical++;
                else if (candidate.Status == MooringSignedCandidateStatus.Indeterminate)
                    indeterminate++;
                else
                    throw new InvalidOperationException($"Signed snapshot integration {name}: unexpected status {candidate.Status}.");

                AssertReadModelEquivalent(name + " legacy preservation", legacy, selected);
                var expectedSource = legacy.UsesDiscreteLoads
                    ? MooringShapeSourceIdentity.IterativeDiscreteSolver
                    : MooringShapeSourceIdentity.FallbackShapeSolver;
                if (selectedCore.SourceIdentity != expectedSource ||
                    selectedCore.SelectedConverged != legacy.Shape.Converged ||
                    selectedCore.SelectedUsesDiscreteLoads != legacy.UsesDiscreteLoads)
                {
                    throw new InvalidOperationException(
                        $"Signed snapshot integration {name}: non-Accepted candidate contaminated selected-core truth.");
                }
                AssertShapeEquivalent(name + " non-Accepted core", legacy.Shape, selectedCore.Shape);
            }

            Console.WriteLine(string.Join("|",
                "SIGNED_CANDIDATE_SNAPSHOT_INTEGRATION",
                name,
                $"CandidateStatus={candidate.Status}",
                $"LegacySource={legacy.Source}",
                $"SelectedSource={selected.Source}",
                $"TypedSource={selectedCore.SourceIdentity}",
                $"AuthoritySwitch={candidate.Status == MooringSignedCandidateStatus.Accepted}"));
        }

        if (total != 5 || accepted != 2 || rejectedPhysical != 2 || indeterminate != 1)
        {
            throw new InvalidOperationException(
                $"Signed snapshot integration truth table mismatch: total={total}, accepted={accepted}, rejectedPhysical={rejectedPhysical}, indeterminate={indeterminate}.");
        }
    }

    private static void AssertReadModelEquivalent(string name, SelectedShapeReadModel expected, SelectedShapeReadModel actual)
    {
        if (expected.Source != actual.Source ||
            expected.UsesDiscreteLoads != actual.UsesDiscreteLoads ||
            expected.HasGateSelection != actual.HasGateSelection ||
            expected.GateDecision != actual.GateDecision ||
            expected.DecisionText != actual.DecisionText ||
            expected.MethodNote != actual.MethodNote)
        {
            throw new InvalidOperationException($"{name}: SelectedShapeReadModel metadata changed.");
        }
        AssertShapeEquivalent(name, expected.Shape, actual.Shape);
    }

    private static void AssertShapeEquivalent(string name, MooringShapeResult expected, MooringShapeResult actual)
    {
        if (expected.BuoyState != actual.BuoyState ||
            expected.DepthM != actual.DepthM || expected.LineLengthM != actual.LineLengthM ||
            expected.HorizontalOffsetM != actual.HorizontalOffsetM || expected.VerticalResidualM != actual.VerticalResidualM ||
            expected.Converged != actual.Converged || expected.MethodNote != actual.MethodNote ||
            expected.IterationCount != actual.IterationCount || expected.ConvergenceResidualM != actual.ConvergenceResidualM ||
            expected.AngleScale != actual.AngleScale || expected.ConvergenceCriterion != actual.ConvergenceCriterion ||
            expected.Nodes.Count != actual.Nodes.Count)
        {
            throw new InvalidOperationException($"{name}: selected shape scalar/metadata state changed.");
        }

        for (var i = 0; i < expected.Nodes.Count; i++)
        {
            var a = expected.Nodes[i];
            var b = actual.Nodes[i];
            if (a.Number != b.Number || a.SegmentNumber != b.SegmentNumber || a.Label != b.Label ||
                a.AlongLineM != b.AlongLineM || a.XOffsetM != b.XOffsetM || a.ZDepthM != b.ZDepthM ||
                a.SegmentLengthM != b.SegmentLengthM || a.SegmentAngleFromVerticalDeg != b.SegmentAngleFromVerticalDeg ||
                a.SegmentTensionKn != b.SegmentTensionKn || a.Status != b.Status)
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
                $"Signed snapshot integration: property {source.GetType().Name}.{propertyName} was not found.");
        var value = property.GetValue(source);
        if (value is T typed)
            return typed;
        throw new InvalidOperationException(
            $"Signed snapshot integration: property {source.GetType().Name}.{propertyName} is not {typeof(T).Name}.");
    }
}
