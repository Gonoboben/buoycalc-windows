using System.Globalization;
using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;

internal static class WaveLoadOwnershipRegression
{
    private const double ToleranceN = 1e-9;

    public static void Validate()
    {
        var seabed = new SeabedPreset("f1a", "F1-A reference seabed", 1.0, "validation only");
        var environment = new EnvironmentInput(
            WaterDensityKgM3: 1025.0,
            DepthM: 50.0,
            CurrentSpeedMS: 0.5,
            WaveHeightM: 1.2,
            WavePeriodS: 6.0,
            Seabed: seabed);
        var buoy = new BuoyInput(
            Name: "F1-A buoy",
            VolumeM3: 0.5,
            WeightKg: 80.0,
            ProjectedAreaM2: 0.5,
            DragCoefficient: 0.8);
        var rope = new RopePreset(
            "f1a-rope", "F1-A rope", "reference", 20.0, 50.0, 0.10, 1.2, "validation only");
        var line = new AssemblyItemInput(
            AssemblyItemKind.Line,
            "Line",
            true,
            rope,
            null,
            55.0,
            1,
            0.0,
            0.0,
            0.0,
            0.0);
        var anchor = new AnchorInput(
            "F1-A anchor",
            "Deadweight",
            "Concrete / Бетон",
            1000.0,
            0.40,
            1.0);

        var baseline = ApplicationCalculationRunner.Run(environment, buoy, new[] { line }, anchor, 5.0);
        var waveVelocity = Math.PI * environment.WaveHeightM / environment.WavePeriodS;
        var expectedWaveForceN = 0.5 * environment.EffectiveWaterDensityKgM3 * waveVelocity * waveVelocity *
                                 buoy.ProjectedAreaM2 * buoy.DragCoefficient;

        Near(baseline.Result.WaveForceN, expectedWaveForceN, "independent buoy wave-drag identity");
        Near(
            baseline.Result.HorizontalForceN,
            baseline.Result.CurrentForceN + baseline.Result.WaveForceN,
            "horizontal current-plus-wave identity");

        var noWave = ApplicationCalculationRunner.Run(
            environment with { WaveHeightM = 0.0 },
            buoy,
            new[] { line },
            anchor,
            5.0);
        Near(noWave.Result.WaveForceN, 0.0, "zero-height wave force");
        Near(noWave.Result.CurrentForceN, baseline.Result.CurrentForceN, "wave does not alter steady current force");
        Near(noWave.Result.HorizontalForceN, noWave.Result.CurrentForceN, "zero-wave horizontal identity");

        var waveOnly = ApplicationCalculationRunner.Run(
            environment with { CurrentSpeedMS = 0.0 },
            buoy,
            new[] { line },
            anchor,
            5.0);
        Near(waveOnly.Result.CurrentForceN, 0.0, "zero-current aggregate current force");
        Near(waveOnly.Result.WaveForceN, baseline.Result.WaveForceN, "wave force independent of scalar current");
        Near(waveOnly.Result.HorizontalForceN, waveOnly.Result.WaveForceN, "wave-only horizontal identity");

        var payload = new AssemblyItemInput(
            AssemblyItemKind.Payload,
            "Instrument",
            true,
            null,
            null,
            0.0,
            1,
            40.0,
            0.004,
            0.12,
            1.0);
        var withPayload = ApplicationCalculationRunner.Run(
            environment,
            buoy,
            new[] { line, payload },
            anchor,
            5.0);
        Near(withPayload.Result.WaveForceN, baseline.Result.WaveForceN, "payload does not duplicate wave force");
        Near(
            withPayload.Result.HorizontalForceN - baseline.Result.HorizontalForceN,
            withPayload.Result.CurrentForceN - baseline.Result.CurrentForceN,
            "payload changes current term only under current wave ownership");

        var alternateRope = rope with { DiameterMm = 35.0, DragCoefficient = 1.4 };
        var alternateLine = line with { RopePreset = alternateRope, LengthM = 60.0 };
        var withDifferentLine = ApplicationCalculationRunner.Run(
            environment,
            buoy,
            new[] { alternateLine },
            anchor,
            5.0);
        Near(withDifferentLine.Result.WaveForceN, baseline.Result.WaveForceN, "line properties do not own WaveForceN");

        Console.WriteLine(string.Join("|",
            "F1A_WAVE_LOAD_OWNERSHIP",
            $"WaveVelocityMS={F(waveVelocity)}",
            $"WaveForceN={F(baseline.Result.WaveForceN)}",
            $"CurrentForceN={F(baseline.Result.CurrentForceN)}",
            $"HorizontalForceN={F(baseline.Result.HorizontalForceN)}",
            "WaveOwner=BuoyProjectedAreaCd",
            "WaveDirection=HorizontalScalar",
            "WaveModel=LegacyDragProxy",
            "VerticalWaveComponent=None",
            "DistributedLineWave=None",
            "InertiaAddedMass=None",
            "TimeDomain=None",
            "ProductionAuthority=LegacyUnchanged"));
        Console.WriteLine(
            "F1A_WAVE_LOAD_OWNERSHIP_ROLLUP|ZeroWaveIdentity=True|WaveOnlyIdentity=True|CurrentPlusWaveIdentity=True|PayloadDoesNotDuplicateWave=True|LineDoesNotOwnWave=True|FutureSemantics=QuasiStaticDesignEnvelope|DynamicClaim=False|ProductionAuthority=LegacyUnchanged");
    }

    private static void Near(double actual, double expected, string label)
    {
        if (!double.IsFinite(actual) || !double.IsFinite(expected) || Math.Abs(actual - expected) > ToleranceN)
        {
            throw new InvalidOperationException(
                $"F1-A {label}: expected {expected:R}, got {actual:R}.");
        }
    }

    private static string F(double value) => value.ToString("R", CultureInfo.InvariantCulture);
}
