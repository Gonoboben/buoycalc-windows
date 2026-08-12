using BuoyCalc.Windows.ApplicationModel;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal static class ForceShapeConsistencyEvidence
{
    internal static void PrintVerticalZeroCurrent()
    {
        var seabed = new SeabedPreset(
            "reg:sand",
            "Regression sand",
            1.2,
            "Deterministic regression seabed preset.");
        var environment = new EnvironmentInput(1025.0, 50.0, 0.0, 0.0, 0.0, seabed);
        var buoy = new BuoyInput("Regression buoy", 1.0, 100.0, 0.8, 0.8);
        var anchor = new AnchorInput(
            "Regression concrete anchor",
            "Concrete block",
            "Concrete",
            1000.0,
            0.4,
            1.0);
        var rope = new RopePreset(
            "reg:heavy-line",
            "Regression heavy line",
            "Polyester",
            20.0,
            100.0,
            0.1,
            1.2,
            "Deterministic heavy-line regression preset.");
        var assembly = new[]
        {
            new AssemblyItemInput(
                AssemblyItemKind.Line,
                "Vertical line",
                true,
                rope,
                null,
                50.0,
                1,
                0,
                0,
                0,
                0)
        };

        var result = BuoyCalculator.Calculate(environment, buoy, assembly, anchor, 3.0);
        var baseTensions = SegmentTensionAnalyzer.Build(result);
        var snapshot = CalculationSnapshotBuilder.Build(environment, result);
        var data = snapshot.TechnicalReportData;
        var consistency = data.ForceShapeConsistency;
        var worst = consistency.Rows
            .Where(x => x.IsAvailable && x.RelativeResidual.HasValue)
            .OrderByDescending(x => x.RelativeResidual!.Value)
            .FirstOrDefault();

        Console.Error.WriteLine("BEGIN_FORCE_SHAPE_VERTICAL_EVIDENCE");
        Console.Error.WriteLine($"LineLengthM={result.LineLengthM:R}");
        Console.Error.WriteLine($"SegmentLengthSumM={result.SegmentRows.Sum(x => x.SegmentLengthM):R}");
        Console.Error.WriteLine($"FallbackAngleScale={data.Shape.AngleScale:R}");
        Console.Error.WriteLine($"FallbackVerticalResidualM={data.Shape.VerticalResidualM:R}");
        Console.Error.WriteLine($"MaxResidualN={consistency.MaxResidualN:R}");
        Console.Error.WriteLine($"MaxRelativeResidual={consistency.MaxRelativeResidual:R}");
        Console.Error.WriteLine($"MaxAngleDifferenceDeg={consistency.MaxAngleDifferenceDeg:R}");

        if (worst is not null)
        {
            var segment = result.SegmentRows.Single(x => x.Number == worst.SegmentNumber);
            var baseTension = baseTensions.Single(x => x.Number == worst.SegmentNumber);
            var shapeNode = data.Shape.Nodes
                .Where(x => x.Number > 0 && x.SegmentNumber == worst.SegmentNumber)
                .Last();

            Console.Error.WriteLine($"WorstSegment={worst.SegmentNumber}");
            Console.Error.WriteLine($"WorstSource={worst.SourceElement}");
            Console.Error.WriteLine($"SegmentCurrentForceN={segment.CurrentForceN:R}");
            Console.Error.WriteLine($"BaseCumulativeH={baseTension.CumulativeHorizontalForceN:R}");
            Console.Error.WriteLine($"BaseCumulativeV={baseTension.CumulativeVerticalForceN:R}");
            Console.Error.WriteLine($"BaseAngleDeg={baseTension.AngleFromVerticalDeg:R}");
            Console.Error.WriteLine($"ShapeNodeAngleDeg={shapeNode.SegmentAngleFromVerticalDeg:R}");
            Console.Error.WriteLine($"dx={worst.DeltaXM:R}");
            Console.Error.WriteLine($"dz={worst.DeltaZM:R}");
            Console.Error.WriteLine($"Lgeom={worst.GeometryLengthM:R}");
            Console.Error.WriteLine($"GeomAngleDeg={worst.GeometricAngleFromVerticalDeg:R}");
            Console.Error.WriteLine($"ForceAngleDeg={worst.ForceAngleFromVerticalDeg:R}");
            Console.Error.WriteLine($"H={worst.ForceHorizontalN:R}");
            Console.Error.WriteLine($"V={worst.ForceVerticalN:R}");
            Console.Error.WriteLine($"T={worst.TensionN:R}");
            Console.Error.WriteLine($"R_H={worst.ResidualHorizontalN:R}");
            Console.Error.WriteLine($"R_V={worst.ResidualVerticalN:R}");
            Console.Error.WriteLine($"R={worst.ResidualN:R}");
            Console.Error.WriteLine($"R_rel={worst.RelativeResidual:R}");
            Console.Error.WriteLine($"DeltaAngleDeg={worst.AngleDifferenceDeg:R}");
        }

        Console.Error.WriteLine("END_FORCE_SHAPE_VERTICAL_EVIDENCE");
    }
}
