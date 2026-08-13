using System.Collections.Generic;
using System.Linq;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

public sealed record SegmentPlanarProjectionReadModel(
    ProfilePlanarProjectionReadModel Projection,
    string AxisProvenance,
    IReadOnlyList<int> SourceSegmentNumbers);

public static class SegmentPlanarProjectionReadModelBuilder
{
    public const string ProjectAxisProvenance = "Project.PlanarXAxisAzimuthDeg";

    public static SegmentPlanarProjectionReadModel Build(
        IReadOnlyList<SegmentCalculationRow> segments,
        double? planarXAxisAzimuthDeg)
    {
        var profile = segments
            .Select(x => new CurrentProfilePointInput(
                x.EstimatedDepthM,
                x.EastCurrentMS,
                x.NorthCurrentMS,
                x.VerticalCurrentMS,
                x.WaterDensityKgM3))
            .ToArray();

        var projection = ProfilePlanarProjectionReadModelBuilder.Build(profile, planarXAxisAzimuthDeg);
        var sourceNumbers = projection.Available
            ? segments.Select(x => x.Number).ToArray()
            : System.Array.Empty<int>();

        return new SegmentPlanarProjectionReadModel(
            projection,
            projection.Available ? ProjectAxisProvenance : string.Empty,
            sourceNumbers);
    }
}
