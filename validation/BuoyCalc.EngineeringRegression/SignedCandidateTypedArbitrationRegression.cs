using System.Collections;
using System.Reflection;
using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal static class SignedCandidateTypedArbitrationRegression
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
                "Typed arbitration: HistoricalGoldenImpactRegression.BuildHistoricalScenarios was not found.");

        var definitions = scenarioBuilder.Invoke(null, null) as IEnumerable
            ?? throw new InvalidOperationException(
                "Typed arbitration: historical fixture definitions are unavailable.");

        var total = 0;
        var accepted = 0;
        var rejectedPhysical = 0;
        var indeterminate = 0;

        foreach (var definition in definitions.Cast<object>())
        {
            var name = RequireProperty<string>(definition, "Name");
            if (!ExpectedStatus.TryGetValue(name, out var expectedStatus))
                throw new InvalidOperationException($"Typed arbitration: unexpected fixture '{name}'.");

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
            var candidate = snapshot.SignedCandidate
                ?? throw new InvalidOperationException($"Typed arbitration {name}: signed candidate is null.");
            var snapshotShadow = snapshot.ShadowSelectedCore
                ?? throw new InvalidOperationException($"Typed arbitration {name}: snapshot shadow is null.");

            if (candidate.Status != expectedStatus)
            {
                throw new InvalidOperationException(
                    $"Typed arbitration {name}: expected {expectedStatus}, got {candidate.Status}.");
            }

            var currentSelection = MooringPrimaryShapeSelector.Select(
                snapshot.TechnicalReportData.Shape,
                snapshot.TechnicalReportData.IterativeSolver);
            if (currentSelection.Shape.Nodes.Count < 2)
            {
                throw new InvalidOperationException(
                    $"Typed arbitration {name}: canonical current selection is not representable as typed selected-core state.");
            }

            var currentSource = currentSelection.UsesDiscreteLoads
                ? MooringShapeSourceIdentity.IterativeDiscreteSolver
                : MooringShapeSourceIdentity.FallbackShapeSolver;
            var currentCore = MooringSelectedShapeResult.Create(
                currentSelection.Shape,
                currentSource,
                currentSelection.Shape.Converged,
                currentSelection.UsesDiscreteLoads,
                "Typed shadow mirror of the existing production primary-shape selection; user-facing authority is unchanged.");

            var direct = MooringSelectedShapeArbitrator.Arbitrate(currentCore, candidate)
                ?? throw new InvalidOperationException($"Typed arbitration {name}: direct result is null.");

            if (candidate.Status == MooringSignedCandidateStatus.Accepted)
            {
                accepted++;
                if (ReferenceEquals(direct, currentCore) ||
                    candidate.Shape is null ||
                    direct.SourceIdentity != MooringShapeSourceIdentity.SignedBoundaryFeedback ||
                    !ReferenceEquals(direct.Shape, candidate.Shape) ||
                    direct.SelectedConverged != candidate.ExactFixedPointReached ||
                    !direct.SelectedConverged ||
                    direct.SelectedUsesDiscreteLoads != candidate.ContainsDiscreteLoads)
                {
                    throw new InvalidOperationException(
                        $"Typed arbitration {name}: Accepted candidate was not selected truthfully.");
                }
            }
            else
            {
                if (!ReferenceEquals(direct, currentCore))
                {
                    throw new InvalidOperationException(
                        $"Typed arbitration {name}: non-Accepted candidate did not preserve the exact current selected-core result.");
                }

                if (MooringSelectedShapeArbitrator.Arbitrate(null, candidate) is not null)
                {
                    throw new InvalidOperationException(
                        $"Typed arbitration {name}: non-Accepted candidate contaminated a null current selection.");
                }

                if (candidate.Status == MooringSignedCandidateStatus.RejectedPhysical)
                    rejectedPhysical++;
                else if (candidate.Status == MooringSignedCandidateStatus.Indeterminate)
                    indeterminate++;
                else
                    throw new InvalidOperationException(
                        $"Typed arbitration {name}: unexpected non-Accepted status {candidate.Status}.");
            }

            AssertEquivalent(name, direct, snapshotShadow);

            var productionSelected = snapshot.SelectedShape
                ?? throw new InvalidOperationException($"Typed arbitration {name}: SelectedShapeReadModel is null.");
            var legacyExpected = SelectedMooringShapeProvider.Build(
                snapshot.TechnicalReportData.Shape,
                snapshot.TechnicalReportData.IterativeSolver);
            if (!ReferenceEquals(productionSelected.Shape, legacyExpected.Shape) &&
                (productionSelected.Source != legacyExpected.Source ||
                 productionSelected.UsesDiscreteLoads != legacyExpected.UsesDiscreteLoads ||
                 productionSelected.Shape.HorizontalOffsetM != legacyExpected.Shape.HorizontalOffsetM ||
                 productionSelected.Shape.Nodes.Count != legacyExpected.Shape.Nodes.Count))
            {
                throw new InvalidOperationException(
                    $"Typed arbitration {name}: user-facing selected-shape authority changed in Package 4.");
            }

            Console.WriteLine(string.Join("|",
                "SIGNED_CANDIDATE_TYPED_ARBITRATION",
                name,
                $"CandidateStatus={candidate.Status}",
                $"CurrentSource={currentCore.SourceIdentity}",
                $"ArbitratedSource={direct.SourceIdentity}",
                $"ArbitratedConverged={direct.SelectedConverged}",
                $"ArbitratedUsesDiscreteLoads={direct.SelectedUsesDiscreteLoads}",
                "ReadModelAuthoritySwitch=False"));
        }

        if (total != 5 || accepted != 2 || rejectedPhysical != 2 || indeterminate != 1)
        {
            throw new InvalidOperationException(
                $"Typed arbitration truth table mismatch: total={total}, accepted={accepted}, rejectedPhysical={rejectedPhysical}, indeterminate={indeterminate}.");
        }
    }

    private static void AssertEquivalent(
        string name,
        MooringSelectedShapeResult expected,
        MooringSelectedShapeResult actual)
    {
        if (expected.SourceIdentity != actual.SourceIdentity ||
            expected.SelectedConverged != actual.SelectedConverged ||
            expected.SelectedUsesDiscreteLoads != actual.SelectedUsesDiscreteLoads ||
            expected.MethodNote != actual.MethodNote ||
            !ReferenceEquals(expected.Shape, actual.Shape))
        {
            throw new InvalidOperationException(
                $"Typed arbitration {name}: snapshot shadow differs from direct typed arbitrator output.");
        }
    }

    private static T RequireProperty<T>(object source, string propertyName)
    {
        var property = source.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                $"Typed arbitration: property {source.GetType().Name}.{propertyName} was not found.");
        var value = property.GetValue(source);
        if (value is T typed)
            return typed;
        throw new InvalidOperationException(
            $"Typed arbitration: property {source.GetType().Name}.{propertyName} is not {typeof(T).Name}.");
    }
}
