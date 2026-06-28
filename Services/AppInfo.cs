namespace BuoyCalc.Windows.Services;

public static class AppInfo
{
    public const string Version = "v0.34";
    public const string VersionNote = "tension report";

    public static string WindowTitle => "BuoyCalc Windows " + Version;
    public static string DisplayVersion => Version + " - " + VersionNote;
}
