namespace BuoyCalc.Windows.Services;

public static class AppInfo
{
    public const string Version = "v0.39";
    public const string VersionNote = "iterative solver skeleton";

    public static string WindowTitle => "BuoyCalc Windows " + Version;
    public static string DisplayVersion => Version + " - " + VersionNote;
}