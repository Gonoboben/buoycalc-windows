using System.Linq;
using Avalonia.Controls;
using Avalonia.VisualTree;
using BuoyCalc.Windows.Services;

namespace BuoyCalc.Windows.Views;

internal static class WindowVersionHelper
{
    private static readonly string[] LegacyVersionTexts =
    {
        "v0.19",
        "v0.21.2",
        "v0.21.3",
        "v0.21.3 cleanup",
        "v0.24.4",
        "v0.36 - alt XZ nodes",
        "v0.37 - 2D comparison",
        "v0.37.1 - build CI",
        "v0.38 - PDF comparison",
        "v0.38.1 - verdict cleanup",
        "v0.38.2 - short line warning",
        "v0.38.3 - CI status bridge"
    };

    public static void Apply(Window window, string titlePrefix)
    {
        window.Title = titlePrefix + " " + AppInfo.Version;
        window.Opened += (_, _) => RefreshBadges(window);
    }

    private static void RefreshBadges(Window window)
    {
        foreach (var textBlock in window.GetVisualDescendants().OfType<TextBlock>())
        {
            if (LegacyVersionTexts.Contains(textBlock.Text))
            {
                textBlock.Text = AppInfo.DisplayVersion;
            }
        }
    }
}
