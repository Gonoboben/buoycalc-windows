namespace BuoyCalc.Windows.Services;

public static class AppInfo
{
    public const string Version = "v0.38.3";
    public const string VersionNote = "CI status bridge";

    public static string WindowTitle => "BuoyCalc Windows " + Version;
    public static string DisplayVersion => Version + " - " + VersionNote;
}
