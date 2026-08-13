using System;
using System.Collections.Generic;
using BuoyCalc.Windows.Models;

namespace BuoyCalc.Windows.Services;

public sealed record ProfilePlanarProjectionReadModel(
    bool Available,
    double? AxisAzimuthDeg,
    IReadOnlyList<ProfilePlanarProjectionRow> Rows);

public sealed record ProfilePlanarProjectionRow(
    int Number,
    double DepthM,
    double EastCurrentMS,
    double NorthCurrentMS,
    double VerticalCurrentMS,
    double UXMS,
    double UZMS,
    double UOutMS,
    double RetainedHorizontalSpeedMS,
    double DiscardedHorizontalSpeedMS);

public static class ProfilePlanarProjectionReadModelBuilder
{
    public static ProfilePlanarProjectionReadModel Build(
        IReadOnlyList<CurrentProfilePointInput> profile,
        double? planarXAxisAzimuthDeg)
    {
        if (planarXAxisAzimuthDeg is null)
            return new(false, null, Array.Empty<ProfilePlanarProjectionRow>());

        var azimuth = planarXAxisAzimuthDeg.Value;
        if (!double.IsFinite(azimuth))
            throw new InvalidOperationException("Planar X-axis azimuth must be finite.");

        var normalized = ((azimuth % 360.0) + 360.0) % 360.0;
        var radians = normalized * Math.PI / 180.0;
        var eEast = Math.Sin(radians);
        var eNorth = Math.Cos(radians);
        var rows = new List<ProfilePlanarProjectionRow>(profile.Count);

        for (var i = 0; i < profile.Count; i++)
        {
            var point = profile[i];
            RequireFinite(point.DepthM);
            RequireFinite(point.EastCurrentMS);
            RequireFinite(point.NorthCurrentMS);
            RequireFinite(point.VerticalCurrentMS);

            var uX = point.EastCurrentMS * eEast + point.NorthCurrentMS * eNorth;
            var uOut = -point.EastCurrentMS * eNorth + point.NorthCurrentMS * eEast;
            rows.Add(new(
                i + 1,
                point.DepthM,
                point.EastCurrentMS,
                point.NorthCurrentMS,
                point.VerticalCurrentMS,
                uX,
                point.VerticalCurrentMS,
                uOut,
                Math.Abs(uX),
                Math.Abs(uOut)));
        }

        return new(true, normalized, rows);
    }

    private static void RequireFinite(double value)
    {
        if (!double.IsFinite(value))
            throw new InvalidOperationException("Profile planar projection inputs must be finite.");
    }
}
