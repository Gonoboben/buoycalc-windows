using System.Collections;
using System.Reflection;
using BuoyCalc.Windows.Models;

internal sealed record AuthorityProofFixture(string Id, EnvironmentInput Environment, BuoyInput Buoy,
    IReadOnlyList<AssemblyItemInput> Assembly, AnchorInput Anchor, double SafetyFactor = 3.0);

internal static class AuthorityProofFixtures
{
    private const BindingFlags PrivateStatic = BindingFlags.NonPublic | BindingFlags.Static;

    internal static IEnumerable<AuthorityProofFixture> Build()
    {
        // Linked actual fixture definitions, including sheared/veering profiles. No copies of their input tables.
        var series = (IReadOnlyList<CampaignScenario>)typeof(Program).GetMethod("BuildScenarios", PrivateStatic)!.Invoke(null, null)!;
        foreach (var scenario in series)
        {
            var (env, buoy, _, anchor) = FixedProgram.BuildInputs(new FixedDiagnosticScenario(scenario.Id, scenario.DepthM, scenario.LineLengthM, 0));
            var rope = new RopePreset("campaign:rope-14", "Series A 14 mm line", "Synthetic reference", 14, 70, 0.15, 1,
                "Deterministic campaign line; not a product recommendation.");
            var assembly = (IReadOnlyList<AssemblyItemInput>)typeof(Program).GetMethod("BuildAssembly", PrivateStatic)!
                .Invoke(null, new object[] { scenario, rope })!;
            yield return new(scenario.Id, env with { CurrentProfile = scenario.CurrentProfile }, buoy, assembly, anchor);
        }
        var historical = (IEnumerable)typeof(HistoricalGoldenImpactRegression).GetMethod("BuildHistoricalScenarios", PrivateStatic)!.Invoke(null, null)!;
        foreach (var row in historical)
        {
            T Get<T>(string name) => (T)row.GetType().GetProperty(name)!.GetValue(row)!;
            yield return new("historical-" + Get<string>("Name"), Get<EnvironmentInput>("Environment"), Get<BuoyInput>("Buoy"),
                Get<IReadOnlyList<AssemblyItemInput>>("Assembly"), Get<AnchorInput>("Anchor"), Get<double>("SafetyFactor"));
        }
        foreach (var depth in new[] { 20.0, 50.0, 100.0, 350.0, 380.0, 500.0 })
        foreach (var ratio in new[] { 1.01, 1.05, 1.1, 1.2 })
        foreach (var speed in new[] { 0.2, 0.45, 0.6 })
        {
            var id = FormattableString.Invariant($"grid-{depth}-{ratio}-{speed}");
            var (env, buoy, assembly, anchor) = FixedProgram.BuildInputs(new FixedDiagnosticScenario(id, depth, depth * ratio, speed));
            yield return new(id, env, buoy, assembly, anchor);
        }
        var (basisEnv, basisBuoy, basisAssembly, basisAnchor) = FixedProgram.BuildInputs(new FixedDiagnosticScenario("adversarial", 20, 22, 0.2));
        foreach (var weight in new[] { -0.15, 0.0, 0.15 })
            yield return new(FormattableString.Invariant($"signed-weight-{weight}"), basisEnv, basisBuoy,
                basisAssembly.Select(a => a with { RopePreset = a.RopePreset! with { WeightWaterKgM = weight } }).ToArray(), basisAnchor);
        foreach (var tiny in new[] { 0.00001, 0.0001, 0.0002 })
            yield return new(FormattableString.Invariant($"tiny-line-{tiny}"), basisEnv, basisBuoy,
                new[] { basisAssembly[0] with { Title = "Tiny line", LengthM = tiny }, basisAssembly[0] with { LengthM = 22 - tiny } }, basisAnchor);
        foreach (var vertical in new[] { -0.05, 0.05 })
            yield return new(FormattableString.Invariant($"vertical-current-{vertical}"), basisEnv with
                { CurrentProfile = basisEnv.CurrentProfile!.Select(p => p with { VerticalCurrentMS = vertical }).ToArray() },
                basisBuoy, basisAssembly, basisAnchor);

        // Focus on neighborhoods of newly exposed arithmetic cycles rather than
        // fitting only the original three residual fixtures.
        foreach (var depth in new[] { 20.0, 380.0 })
        foreach (var ratio in new[] { 1.04, 1.05, 1.06, 1.19, 1.20, 1.21 })
        foreach (var speed in new[] { 0.55, 0.60, 0.65 })
        {
            var id = FormattableString.Invariant($"neighborhood-{depth}-{ratio}-{speed}");
            var (env, buoy, assembly, anchor) = FixedProgram.BuildInputs(new FixedDiagnosticScenario(id, depth, depth * ratio, speed));
            yield return new(id, env, buoy, assembly, anchor);
        }
        foreach (var ratio in new[] { 1.09, 1.10, 1.11 })
        foreach (var speed in new[] { 0.40, 0.45, 0.50 })
        {
            var id = FormattableString.Invariant($"deep-neighborhood-{ratio}-{speed}");
            var (env, buoy, assembly, anchor) = FixedProgram.BuildInputs(new FixedDiagnosticScenario(id, 380, 380 * ratio, speed));
            yield return new(id, env, buoy, assembly, anchor);
        }
        var (deepEnv, deepBuoy, deepAssembly, deepAnchor) = FixedProgram.BuildInputs(new FixedDiagnosticScenario("deep", 380, 418, 0.45));
        foreach (var cd in new[] { 0.5, 1.2, 2.0 })
        foreach (var diameter in new[] { 6.0, 14.0, 20.0 })
        foreach (var density in new[] { 1000.0, 1030.0 })
            yield return new(FormattableString.Invariant($"coeff-{cd}-{diameter}-{density}"), deepEnv with
                { WaterDensityKgM3 = density, CurrentProfile = deepEnv.CurrentProfile!.Select(p => p with { WaterDensityKgM3 = density }).ToArray() },
                deepBuoy, deepAssembly.Select(a => a with { RopePreset = a.RopePreset! with { DragCoefficient = cd, DiameterMm = diameter } }).ToArray(), deepAnchor);
        foreach (var height in new[] { 0.5, 1.5 })
        foreach (var period in new[] { 5.0, 8.0 })
            yield return new(FormattableString.Invariant($"wave-proxy-{height}-{period}"),
                deepEnv with { WaveHeightM = height, WavePeriodS = period }, deepBuoy, deepAssembly, deepAnchor);
        foreach (var vertical in new[] { -0.05, 0.05 })
            yield return new(FormattableString.Invariant($"deep-vertical-{vertical}"), deepEnv with
                { CurrentProfile = deepEnv.CurrentProfile!.Select(p => p with { VerticalCurrentMS = vertical }).ToArray() },
                deepBuoy, deepAssembly, deepAnchor);
    }
}
