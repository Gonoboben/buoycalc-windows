using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

internal static class SegmentPlanarProjectionRegression
{
    public static void Validate()
    {
        var segment = new SegmentCalculationRow(
            7, "Synthetic", "Synthetic", 0, 1, 1, 12,
            0.6, -0.2, 0.1, 0.65, 1025, 0.01, 1.2, 0, 0);

        var missing = SegmentPlanarProjectionReadModelBuilder.Build(new[] { segment }, null);
        if (missing.Projection.Available || missing.SourceSegmentNumbers.Count != 0)
            throw new InvalidOperationException("Unexpected segment projection availability.");

        var result = SegmentPlanarProjectionReadModelBuilder.Build(new[] { segment }, 90.0);
        if (!result.Projection.Available || result.Projection.Rows.Count != 1)
            throw new InvalidOperationException("Segment projection row missing.");
        if (result.AxisProvenance != SegmentPlanarProjectionReadModelBuilder.ProjectAxisProvenance)
            throw new InvalidOperationException("Axis provenance mismatch.");
        if (result.SourceSegmentNumbers.Count != 1 || result.SourceSegmentNumbers[0] != 7)
            throw new InvalidOperationException("Segment source mismatch.");

        var row = result.Projection.Rows[0];
        Near(12, row.DepthM);
        Near(0.6, row.UXMS);
        Near(-0.2, row.UOutMS);
        Near(0.1, row.UZMS);
    }

    private static void Near(double expected, double actual)
    {
        if (Math.Abs(expected - actual) > 1e-12)
            throw new InvalidOperationException("Segment projection value mismatch.");
    }
}
