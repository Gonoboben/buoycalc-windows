using System.Collections;
using System.Globalization;
using System.Reflection;
using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal static class SignedScalarDivergenceEvidenceRegression
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
                "Signed scalar divergence evidence: HistoricalGoldenImpactRegression.BuildHistoricalScenarios was not found.");
        var definitions = scenarioBuilder.Invoke(null, null) as IEnumerable
            ?? throw new InvalidOperationException(
                "Signed scalar divergence evidence: historical fixtures are unavailable.");

        var total = 0;
        var signedAvailable = 0;
        var signedUnavailable = 0;
        var accepted = 0;
        var rejectedPhysical = 0;
        var indeterminate = 0;

        Console.WriteLine("SIGNED_SCALAR_DIVERGENCE_BEGIN");

        foreach (var definition in definitions.Cast<object>())
        {
            var name = RequireProperty<string>(definition, "Name");
            if (!ExpectedStatus.TryGetValue(name, out var expectedStatus))
                throw new InvalidOperationException($"Signed scalar divergence evidence: unexpected fixture '{name}'.");

            var environment = RequireProperty<EnvironmentInput>(definition, "Environment");
            var buoy = RequireProperty<BuoyInput>(definition, "Buoy");
            var assembly = RequireProperty<IReadOnlyList<AssemblyItemInput>>(definition, "Assembly");
            var anchor = RequireProperty<AnchorInput>(definition, "Anchor");
            var safetyFactor = RequireProperty<double>(definition, "SafetyFactor");

            var run = ApplicationCalculationRunner.Run(environment, buoy, assembly, anchor, safetyFactor);
            var result = run.Result;
            var snapshot = run.Snapshot;
            var candidate = snapshot.SignedCandidate
                ?? throw new InvalidOperationException($"Signed scalar divergence evidence {name}: signed candidate is null.");
            var selected = snapshot.SelectedShape
                ?? throw new InvalidOperationException($"Signed scalar divergence evidence {name}: selected read model is null.");
            var selectedCore = snapshot.ShadowSelectedCore;
            var signedState = MooringSelectedSignedBoundaryStateProjector.Project(selectedCore, candidate);

            if (candidate.Status != expectedStatus)
            {
                throw new InvalidOperationException(
                    $"Signed scalar divergence evidence {name}: expected {expectedStatus}, got {candidate.Status}.");
            }

            RequireFiniteLegacyScalars(name, result, selected.Shape.HorizontalOffsetM);

            total++;
            switch (candidate.Status)
            {
                case MooringSignedCandidateStatus.Accepted:
                    accepted++;
                    signedAvailable++;
                    if (signedState is null ||
                        selectedCore is null ||
                        selectedCore.SourceIdentity != MooringShapeSourceIdentity.SignedBoundaryFeedback ||
                        selected.Source != MooringShapeSourceIdentity.SignedBoundaryFeedback.ToString())
                    {
                        throw new InvalidOperationException(
                            $"Signed scalar divergence evidence {name}: Accepted signed selected source has no direct boundary-state evidence.");
                    }
                    if (signedState.EndpointXM != selected.Shape.HorizontalOffsetM)
                    {
                        throw new InvalidOperationException(
                            $"Signed scalar divergence evidence {name}: selected endpoint X differs from direct signed boundary endpoint X.");
                    }
                    break;

                case MooringSignedCandidateStatus.RejectedPhysical:
                    rejectedPhysical++;
                    signedUnavailable++;
                    RequireNoSignedState(name, signedState, selected);
                    break;

                case MooringSignedCandidateStatus.Indeterminate:
                    indeterminate++;
                    signedUnavailable++;
                    RequireNoSignedState(name, signedState, selected);
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Signed scalar divergence evidence {name}: unexpected canonical status {candidate.Status}.");
            }

            Console.WriteLine(Evidence(name, result, selected, candidate, signedState));
        }

        if (total != 5 || signedAvailable != 2 || signedUnavailable != 3 ||
            accepted != 2 || rejectedPhysical != 2 || indeterminate != 1)
        {
            throw new InvalidOperationException(
                $"Signed scalar divergence evidence truth table mismatch: total={total}, available={signedAvailable}, unavailable={signedUnavailable}, accepted={accepted}, rejectedPhysical={rejectedPhysical}, indeterminate={indeterminate}.");
        }

        Console.WriteLine(string.Join("|",
            "SIGNED_SCALAR_DIVERGENCE_ROLLUP",
            "Scenarios=5",
            "SignedStateAvailable=2",
            "SignedStateUnavailable=3",
            "Accepted=2",
            "RejectedPhysical=2",
            "Indeterminate=1",
            "TensionMagnitudeComparison=NotPerformed",
            "TraceReconstruction=None",
            "EstimatedOffsetVsEndpointX=NotSemanticallyComparable",
            "AnchorDemandTransfer=NotPerformed",
            "ScalarAuthority=LegacyUnchanged",
            "GoldenBaselineModified=False"));
        Console.WriteLine("SIGNED_SCALAR_DIVERGENCE_END");
    }

    private static void RequireNoSignedState(
        string name,
        MooringSelectedSignedBoundaryState? signedState,
        SelectedShapeReadModel selected)
    {
        if (signedState is not null ||
            selected.Source == MooringShapeSourceIdentity.SignedBoundaryFeedback.ToString())
        {
            throw new InvalidOperationException(
                $"Signed scalar divergence evidence {name}: non-Accepted fixture exposed signed selected authority/state.");
        }
    }

    private static void RequireFiniteLegacyScalars(
        string name,
        CalculationResult result,
        double selectedEndpointXM)
    {
        var values = new[]
        {
            result.TensionKn,
            result.RequiredAnchorHoldingKg,
            result.AnchorReserve,
            result.TensionReserve,
            result.EstimatedOffsetM,
            selectedEndpointXM
        };

        if (values.Any(value => !double.IsFinite(value)))
        {
            throw new InvalidOperationException(
                $"Signed scalar divergence evidence {name}: legacy scalar/selected endpoint evidence contains a non-finite value.");
        }
    }

    private static string Evidence(
        string name,
        CalculationResult result,
        SelectedShapeReadModel selected,
        MooringSignedCandidateResult candidate,
        MooringSelectedSignedBoundaryState? signedState)
    {
        return string.Join("|",
            "SIGNED_SCALAR_DIVERGENCE",
            name,
            $"CandidateStatus={candidate.Status}",
            $"SelectedSource={selected.Source}",
            $"SignedStateAvailable={signedState is not null}",
            $"LegacyTensionKn={F(result.TensionKn)}",
            $"BoundaryQ0N={Maybe(signedState?.Q0N)}",
            $"BoundaryBuoySteadyDragN={Maybe(signedState?.BuoySteadyDragN)}",
            $"BoundaryEndHN={Maybe(signedState?.EndHN)}",
            $"BoundaryEndVN={Maybe(signedState?.EndVN)}",
            $"LegacyEstimatedOffsetM={F(result.EstimatedOffsetM)}",
            $"SelectedEndpointX={F(selected.Shape.HorizontalOffsetM)}",
            $"LegacyRequiredAnchorHoldingKg={F(result.RequiredAnchorHoldingKg)}",
            $"LegacyAnchorReserve={F(result.AnchorReserve)}",
            $"LegacyTensionReserve={F(result.TensionReserve)}",
            $"LegacyVerdict={result.Verdict}",
            "TensionVsBoundaryComponents=NotSemanticallyComparable",
            "EstimatedOffsetVsEndpointX=NotSemanticallyComparable",
            "AnchorDemandVsBoundaryComponents=RequiresIndependentAnchorEndValidation",
            "TraceReconstruction=None",
            "ScalarAuthority=LegacyUnchanged");
    }

    private static string Maybe(double? value) => value.HasValue ? F(value.Value) : "NA";

    private static string F(double value) => value.ToString("R", CultureInfo.InvariantCulture);

    private static T RequireProperty<T>(object source, string propertyName)
    {
        var property = source.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                $"Signed scalar divergence evidence: property {source.GetType().Name}.{propertyName} was not found.");
        var value = property.GetValue(source);
        if (value is T typed)
            return typed;
        throw new InvalidOperationException(
            $"Signed scalar divergence evidence: property {source.GetType().Name}.{propertyName} is not {typeof(T).Name}.");
    }
}
