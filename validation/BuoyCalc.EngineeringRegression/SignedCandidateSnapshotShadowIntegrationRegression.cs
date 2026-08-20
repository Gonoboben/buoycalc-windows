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
                "Signed snapshot shadow: HistoricalGoldenImpactRegression.BuildHistoricalScenarios was not found.");

        var definitions = scenarioBuilder.Invoke(null, null) as IEnumerable
            ?? throw new InvalidOperationException(
                "Signed snapshot shadow: historical fixture definitions are unavailable.");

        var total = 0;
        var accepted = 0;
        var rejectedPhysical = 0;
        var indeterminate = 0;

        foreach (var definition in definitions.Cast<object>())
        {
            var name = RequireProperty<string>(definition, "Name");
            if (!ExpectedStatus.TryGetValue(name, out var expectedStatus))
                throw new InvalidOperationException($"Signed snapshot shadow: unexpected fixture '{name}'.");

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
                ?? throw new InvalidOperationException($"Signed snapshot shadow {name}: SelectedShape is null.");
            var candidate = snapshot.SignedCandidate
                ?? throw new InvalidOperationException($"Signed snapshot shadow {name}: SignedCandidate is null.");
            var shadow = snapshot.ShadowSelectedCore
                ?? throw new InvalidOperationException($"Signed snapshot shadow {name}: ShadowSelectedCore is null.");

            if (candidate.Status != expectedStatus)
            {
                throw new InvalidOperationException(
                    $"Signed snapshot shadow {name}: expected candidate {expectedStatus}, got {candidate.Status}.");
            }

            // Rebuild the pre-Package-3 user-facing path independently from the immutable
            // TechnicalReportData and require field/node equivalence. Shadow state must not
            // be an input to this provider.
            var legacyExpected = SelectedMooringShapeProvider.Build(
                snapshot.TechnicalReportData.Shape,
                snapshot.TechnicalReportData.IterativeSolver);
            AssertReadModelEquivalent(name, legacyExpected, selected);

            if (candidate.Status == MooringSignedCandidateStatus.Accepted)
            {
                accepted++;
                if (candidate.Shape is null ||
                    shadow.SourceIdentity != MooringShapeSourceIdentity.SignedBoundaryFeedback ||
                    !shadow.SelectedConverged ||
                    shadow.SelectedUsesDiscreteLoads != candidate.ContainsDiscreteLoads ||
                    !ReferenceEquals(shadow.Shape, candidate.Shape))
                {
                    throw new InvalidOperationException(
                        $"Signed snapshot shadow {name}: Accepted candidate is not represented truthfully by the shadow selected-core result.");
                }
            }
            else
            {
                if (candidate.Status == MooringSignedCandidateStatus.RejectedPhysical)
                    rejectedPhysical++;
                else if (candidate.Status == MooringSignedCandidateStatus.Indeterminate)
                    indeterminate++;
                else
                    throw new InvalidOperationException(
                        $"Signed snapshot shadow {name}: unexpected non-Accepted status {candidate.Status}.");

                var expectedSource = selected.UsesDiscreteLoads
                    ? MooringShapeSourceIdentity.IterativeDiscreteSolver
                    : MooringShapeSourceIdentity.FallbackShapeSolver;
                if (shadow.SourceIdentity != expectedSource ||
                    shadow.SelectedConverged != selected.Shape.Converged ||
                    shadow.SelectedUsesDiscreteLoads != selected.UsesDiscreteLoads)
                {
                    throw new InvalidOperationException(
                        $"Signed snapshot shadow {name}: non-Accepted candidate contaminated current selected-source truth.");
                }
                AssertShapeEquivalent(name + " non-Accepted shadow", selected.Shape, shadow.Shape);
            }

            // The shadow can differ from current production authority for Accepted cases,
            // but the actual read model must still carry the legacy source identity.
            if (candidate.Status == MooringSignedCandidateStatus.Accepted &&
                string.Equals(selected.Source, "SignedBoundaryFeedback", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Signed snapshot shadow {name}: current SelectedShapeReadModel was switched to signed authority prematurely.");
            }

            Console.WriteLine(string.Join("|",
                "SIGNED_CANDIDATE_SNAPSHOT_SHADOW",
                name,
                $"CandidateStatus={candidate.Status}",
                $"ProductionSource={selected.Source}",
                $"ProductionUsesDiscreteLoads={selected.UsesDiscreteLoads}",
                $"ShadowSource={shadow.SourceIdentity}",
                $"ShadowConverged={shadow.SelectedConverged}",
                $"ShadowUsesDiscreteLoads={shadow.SelectedUsesDiscreteLoads}",
                "ReadModelFieldEquivalent=True",
                "AuthoritySwitch=False"));
        }

        if (total != 5 || accepted != 2 || rejectedPhysical != 2 || indeterminate != 1)
        {
            throw new InvalidOperationException(
                $"Signed snapshot shadow truth table mismatch: total={total}, accepted={accepted}, rejectedPhysical={rejectedPhysical}, indeterminate={indeterminate}.");
        }

        Console.WriteLine(string.Join("|",
            "SIGNED_CANDIDATE_SNAPSHOT_SHADOW_ROLLUP",
            $"Scenarios={total}",
            $"Accepted={accepted}",
            $"RejectedPhysical={rejectedPhysical}",
            $"Indeterminate={indeterminate}",
            "SnapshotCandidateRetained=True",
            "TypedShadowRetained=True",
            "SelectedShapeReadModelFieldEquivalent=True",
            "GoldenBaselineModified=False",
            "AuthoritySwitch=False"));
    }

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
            throw new InvalidOperationException(
                $"Signed snapshot shadow {name}: SelectedShapeReadModel metadata changed.");
        }

        AssertShapeEquivalent(name + " read model", expected.Shape, actual.Shape);
    }

    private static void AssertShapeEquivalent(
        string label,
        MooringShapeResult expected,
        MooringShapeResult actual)
    {
        if (expected.BuoyState != actual.BuoyState ||
            expected.DepthM != actual.DepthM ||
            expected.LineLengthM != actual.LineLengthM ||
            expected.HorizontalOffsetM != actual.HorizontalOffsetM ||
            expected.VerticalResidualM != actual.VerticalResidualM ||
            expected.Converged != actual.Converged ||
            !string.Equals(expected.MethodNote, actual.MethodNote, StringComparison.Ordinal) ||
            expected.IterationCount != actual.IterationCount ||
            expected.ConvergenceResidualM != actual.ConvergenceResidualM ||
            expected.AngleScale != actual.AngleScale ||
            !string.Equals(expected.ConvergenceCriterion, actual.ConvergenceCriterion, StringComparison.Ordinal) ||
            expected.Nodes.Count != actual.Nodes.Count)
        {
            throw new InvalidOperationException(
                $"Signed snapshot shadow {label}: selected shape scalar/metadata state changed.");
        }

        for (var i = 0; i < expected.Nodes.Count; i++)
        {
            var left = expected.Nodes[i];
            var right = actual.Nodes[i];
            if (left.Number != right.Number ||
                left.SegmentNumber != right.SegmentNumber ||
                !string.Equals(left.Label, right.Label, StringComparison.Ordinal) ||
                left.AlongLineM != right.AlongLineM ||
                left.XOffsetM != right.XOffsetM ||
                left.ZDepthM != right.ZDepthM ||
                left.SegmentLengthM != right.SegmentLengthM ||
                left.SegmentAngleFromVerticalDeg != right.SegmentAngleFromVerticalDeg ||
                left.SegmentTensionKn != right.SegmentTensionKn ||
                !string.Equals(left.Status, right.Status, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Signed snapshot shadow {label}: selected node {i} changed.");
            }
        }

        AssertOptionalPointEquivalent(label + " buoy", expected.BuoyPoint, actual.BuoyPoint);
        AssertOptionalPointEquivalent(label + " anchor", expected.AnchorPoint, actual.AnchorPoint);
    }

    private static void AssertOptionalPointEquivalent(
        string label,
        MooringShapePoint? expected,
        MooringShapePoint? actual)
    {
        if (expected is null || actual is null)
        {
            if (expected is not null || actual is not null)
                throw new InvalidOperationException($"Signed snapshot shadow {label}: point nullability changed.");
            return;
        }

        if (expected.Number != actual.Number ||
            expected.SegmentNumber != actual.SegmentNumber ||
            !string.Equals(expected.Label, actual.Label, StringComparison.Ordinal) ||
            expected.AlongLineM != actual.AlongLineM ||
            expected.XOffsetM != actual.XOffsetM ||
            expected.ZDepthM != actual.ZDepthM ||
            expected.SegmentLengthM != actual.SegmentLengthM ||
            expected.SegmentAngleFromVerticalDeg != actual.SegmentAngleFromVerticalDeg ||
            expected.SegmentTensionKn != actual.SegmentTensionKn ||
            !string.Equals(expected.Status, actual.Status, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Signed snapshot shadow {label}: point changed.");
        }
    }

    private static T RequireProperty<T>(object source, string propertyName)
    {
        var property = source.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                $"Signed snapshot shadow: property {source.GetType().Name}.{propertyName} was not found.");
        var value = property.GetValue(source);
        if (value is T typed)
            return typed;
        throw new InvalidOperationException(
            $"Signed snapshot shadow: property {source.GetType().Name}.{propertyName} is not {typeof(T).Name}.");
    }
}
