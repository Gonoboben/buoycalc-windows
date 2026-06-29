namespace BuoyCalc.Windows.Services;

public static class AppInfo
{
    public const string Version = "v0.41";
    public const string VersionNote = "deployment modes";

    public static string WindowTitle => "BuoyCalc Windows " + Version;
    public static string DisplayVersion => Version + " - " + VersionNote;
}