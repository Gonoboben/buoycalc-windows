namespace BuoyCalc.Windows.Services;

public static class AppInfo
{
    public const string Version = "v0.29";
    public const string VersionNote = "iterative shape solver";

    public static string WindowTitle => $"BuoyCalc Windows {Version}";
    public static string DisplayVersion => $"{Version} · {VersionNote}";
}
