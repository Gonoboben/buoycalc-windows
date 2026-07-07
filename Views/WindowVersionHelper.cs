using Avalonia.Controls;
using BuoyCalc.Windows.Services;

namespace BuoyCalc.Windows.Views;

internal static class WindowVersionHelper
{
    public static void Apply(Window window, string titlePrefix)
    {
        window.Title = titlePrefix + " " + AppInfo.Version;
    }
}
