using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using BuoyCalc.Windows.Models;
using BuoyCalc.Windows.Services;

namespace BuoyCalc.Windows.Views;

public sealed class ProfilePointPlanarProjectionConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 4)
            return "INFO-проекция: данные недоступны; на solver не влияет.";

        var axisText = values[3]?.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(axisText))
            return "INFO-проекция: ось +X не задана, Ux/Uout недоступны; на solver не влияет.";

        if (!TryFinite(axisText, out var axis))
            return "INFO-проекция: азимут оси +X должен быть конечным числом; на solver не влияет.";

        if (!TryFinite(values[0]?.ToString(), out var east) ||
            !TryFinite(values[1]?.ToString(), out var north) ||
            !TryFinite(values[2]?.ToString(), out var vertical))
            return "INFO-проекция: задайте конечные числовые U/V/W; на solver не влияет.";

        try
        {
            var projection = ProfilePlanarProjectionReadModelBuilder.Build(
                new[] { new CurrentProfilePointInput(0.0, east, north, vertical, 1025.0) }, axis);
            var row = projection.Rows[0];
            return string.Format(culture,
                "INFO-проекция: Ux={0:0.###} · Uz={1:0.###} (+ вниз) · Uout={2:0.###} м/с · ось +X={3:0.##}° · на solver не влияет",
                row.UXMS, row.UZMS, row.UOutMS, projection.AxisAzimuthDeg);
        }
        catch
        {
            return "INFO-проекция: недоступна для текущих данных; на solver не влияет.";
        }
    }

    private static bool TryFinite(string? text, out double value)
    {
        text = (text ?? string.Empty).Replace(',', '.');
        return double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out value) && double.IsFinite(value);
    }
}
