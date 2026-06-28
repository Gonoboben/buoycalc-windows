using System.Linq;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace BuoyCalc.Windows.Views;

internal static class WindowVersionHelper
{
    private const string CurrentVersion = "v0.33";
    private const string CurrentVersionNote = "sequence positions";

    private static readonly string[] LegacyVersionTexts =
    {
        "v0.19",
        "v0.21.2",
        "v0.21.3",
        "v0.21.3 cleanup",
        "v0.24.4"
    };

    public static void Apply(Window window, string titlePrefix)
    {
        window.Title = titlePrefix + " " + CurrentVersion;
        window.Opened += (_, _) => RefreshBadges(window);
    }

    private static void RefreshBadges(Window window)
    {
        foreach (var textBlock in window.GetVisualDescendants().OfType<TextBlock>())
        {
            if (LegacyVersionTexts.Contains(textBlock.Text))
            {
                textBlock.Text = CurrentVersion + " - " + CurrentVersionNote;
            }
        }
    }
}
