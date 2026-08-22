using System.Collections;
using System.Globalization;
using System.Reflection;
using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal static class SelectedSignedBoundaryStateAvailabilityRegression
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

    private static readonly string[] ForbiddenScalarAliases =
    {
        "TensionKn",
        "AnchorReserve",
        "RequiredAnchorHoldingKg",
        "EstimatedOffsetM",
        "TensionReserve",
        "Verdict",
        "MainRisk"
    };

    public static void Validate()
    {
        ValidateContractSurface();

        var scenarioBuilder = typeof(HistoricalGoldenImpactRegression).GetMethod(
            "BuildHistoricalScenarios",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException(
                "Selected signed boundary state: HistoricalGoldenImpactRegression.BuildHistoricalScenarios was not found.");
        var definitions = scenarioBuilder.Invoke(null, null) as IEnumerable
            ?? throw new InvalidOperationException(
                "Selected signed boundary state: historical fixtures are unavailable.");

        var total = 0;
        var available = 0;
        var accepted = 0;
        var rejectedPhysical = 0;
        var indeterminate = 0;

        foreach (var definition in definitions.Cast<object>())
        {
            var name = RequireProperty<string>(definition, "Name");
            if (!ExpectedStatus.TryGetValue(name, out var expectedStatus))
                throw new InvalidOperationException($"Selected signed boundary state: unexpected fixture '{name}'.");

            total++;
            var environment = RequireProperty<EnvironmentInput>(definition, "Environment");
            var buoy = RequireProperty<BuoyInput>(definition, "Buoy");
            var assembly = RequireProperty<IReadOnlyList<AssemblyItemInput>>(definition, "Assembly");
            var anchor = RequireProperty<AnchorInput>(definition, "Anchor");
            var safetyFactor = RequireProperty<double>(definition, "SafetyFactor");

            var run = ApplicationCalculationRunner.Run(environment, buoy, assembly, anchor, safetyFactor);
            var candidate = run.Snapshot.SignedCandidate
                ?? throw new InvalidOperationException($"Selected signed boundary state {name}: candidate is null.");
            var selectedCore = run.Snapshot.ShadowSelectedCore;

            if (candidate.Status != expectedStatus)
            {
                throw new InvalidOperationException(
                    $"Selected signed boundary state {name}: expected {expectedStatus}, got {candidate.Status}.");
            }

            var state = MooringSelectedSignedBoundaryStateProjector.Project(selectedCore, candidate);

            if (candidate.Status == MooringSignedCandidateStatus.Accepted)
            {
                accepted++;
                available++;
                ValidateAccepted(name, selectedCore, candidate, state);
            }
            else
            {
                if (state is not null)
                {
                    throw new InvalidOperationException(
                        $"Selected signed boundary state {name}: non-Accepted/non-signed selected source exposed a boundary state.");
                }

                if (candidate.Status == MooringSignedCandidateStatus.RejectedPhysical)
                    rejectedPhysical++;
                else if (candidate.Status == MooringSignedCandidateStatus.Indeterminate)
                    indeterminate++;
                else
                    throw new InvalidOperationException(
                        $"Selected signed boundary state {name}: unexpected non-Accepted status {candidate.Status}.");
            }

            Console.WriteLine(Evidence(name, candidate, state));
        }

        if (total != 5 || available != 2 || accepted != 2 || rejectedPhysical != 2 || indeterminate != 1)
        {
            throw new InvalidOperationException(
                $"Selected signed boundary state truth table mismatch: total={total}, available={available}, accepted={accepted}, rejectedPhysical={rejectedPhysical}, indeterminate={indeterminate}.");
        }

        Console.WriteLine(
            "SELECTED_SIGNED_BOUNDARY_STATE_ROLLUP|Scenarios=5|Available=2|Unavailable=3|Accepted=2|RejectedPhysical=2|Indeterminate=1|ScalarAuthority=LegacyUnchanged|TraceReconstruction=None");
    }

    private static void ValidateAccepted(
        string name,
        MooringSelectedShapeResult? selectedCore,
        MooringSignedCandidateResult candidate,
        MooringSelectedSignedBoundaryState? state)
    {
        if (selectedCore is null ||
            selectedCore.SourceIdentity != MooringShapeSourceIdentity.SignedBoundaryFeedback ||
            candidate.Shape is null ||
            candidate.Boundary is null ||
            candidate.Boundary.SolutionState is null ||
            state is null)
        {
            throw new InvalidOperationException(
                $"Selected signed boundary state {name}: Accepted selected signed state is incomplete.");
        }

        var boundary = candidate.Boundary;
        var solution = boundary.SolutionState;

        if (!ReferenceEquals(selectedCore.Shape, candidate.Shape) ||
            state.SourceIdentity != MooringShapeSourceIdentity.SignedBoundaryFeedback ||
            state.BoundaryClassification != boundary.Classification ||
            state.Q0N != boundary.Q0N ||
            state.BuoySteadyDragN != boundary.BuoySteadyDragN ||
            state.EndpointXM != solution.EndpointXM ||
            state.EndpointZM != solution.EndpointZM ||
            state.EndHN != solution.EndHN ||
            state.EndVN != solution.EndVN ||
            state.MinHN != solution.MinHN ||
            state.MaxHN != solution.MaxHN ||
            state.MinVN != solution.MinVN ||
            state.MaxVN != solution.MaxVN ||
            state.VSignChange != solution.VSignChange ||
            state.PointLoadCrossings != solution.PointLoadCrossings ||
            state.PointLoadCrossings != candidate.PointLoadCrossings ||
            state.FeedbackIterations != candidate.FeedbackIterations ||
            state.ContainsDiscreteLoads != candidate.ContainsDiscreteLoads ||
            state.BoundaryMethodNote != boundary.MethodNote ||
            state.CandidateDiagnosticCode != candidate.DiagnosticCode ||
            state.CandidateDiagnosticText != candidate.DiagnosticText)
        {
            throw new InvalidOperationException(
                $"Selected signed boundary state {name}: projected state differs from direct Accepted candidate/boundary values.");
        }

        var selectedAnchor = selectedCore.Shape.AnchorPoint
            ?? throw new InvalidOperationException(
                $"Selected signed boundary state {name}: selected signed shape has no endpoint.");
        if (state.EndpointXM != selectedAnchor.XOffsetM ||
            state.EndpointZM != selectedAnchor.ZDepthM ||
            state.EndpointXM != selectedCore.Shape.HorizontalOffsetM)
        {
            throw new InvalidOperationException(
                $"Selected signed boundary state {name}: direct boundary endpoint differs from selected signed geometry.");
        }
    }

    private static void ValidateContractSurface()
    {
        var type = typeof(MooringSelectedSignedBoundaryState);
        foreach (var forbidden in ForbiddenScalarAliases)
        {
            if (type.GetProperty(forbidden, BindingFlags.Instance | BindingFlags.Public) is not null)
            {
                throw new InvalidOperationException(
                    $"Selected signed boundary state contract must not expose downstream scalar alias '{forbidden}'.");
            }
        }

        if (type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Any(property => property.PropertyType == typeof(MooringSurfaceBoundaryTensionTraceResult)))
        {
            throw new InvalidOperationException(
                "Selected signed boundary state contract must not embed a reconstructed tension trace.");
        }
    }

    private static string Evidence(
        string name,
        MooringSignedCandidateResult candidate,
        MooringSelectedSignedBoundaryState? state)
    {
        if (state is null)
        {
            return string.Join("|",
                "SELECTED_SIGNED_BOUNDARY_STATE",
                name,
                $"CandidateStatus={candidate.Status}",
                "Available=False",
                "Source=None",
                "Q0N=NA",
                "BuoySteadyDragN=NA",
                "EndpointX=NA",
                "EndpointZ=NA",
                "EndHN=NA",
                "EndVN=NA",
                $"PointLoadCrossings={candidate.PointLoadCrossings}",
                "ScalarAuthority=LegacyUnchanged",
                "TraceReconstruction=None");
        }

        return string.Join("|",
            "SELECTED_SIGNED_BOUNDARY_STATE",
            name,
            $"CandidateStatus={candidate.Status}",
            "Available=True",
            $"Source={state.SourceIdentity}",
            $"Q0N={F(state.Q0N)}",
            $"BuoySteadyDragN={F(state.BuoySteadyDragN)}",
            $"EndpointX={F(state.EndpointXM)}",
            $"EndpointZ={F(state.EndpointZM)}",
            $"EndHN={F(state.EndHN)}",
            $"EndVN={F(state.EndVN)}",
            $"PointLoadCrossings={state.PointLoadCrossings}",
            "ScalarAuthority=LegacyUnchanged",
            "TraceReconstruction=None");
    }

    private static string F(double value) => value.ToString("R", CultureInfo.InvariantCulture);

    private static T RequireProperty<T>(object source, string propertyName)
    {
        var property = source.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(
                $"Selected signed boundary state: property {source.GetType().Name}.{propertyName} was not found.");
        var value = property.GetValue(source);
        if (value is T typed)
            return typed;
        throw new InvalidOperationException(
            $"Selected signed boundary state: property {source.GetType().Name}.{propertyName} is not {typeof(T).Name}.");
    }
}
